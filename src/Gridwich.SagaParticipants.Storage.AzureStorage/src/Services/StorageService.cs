using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.Exceptions;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Newtonsoft.Json.Linq;
using RangeTree;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Services
{
    /// <summary>
    /// Provides various Azure Storage access methods.
    /// </summary>
    public sealed class StorageService : IStorageService, IDisposable
    {
        private const long MB = 1024 * 1024;

        /// <summary>
        /// Don't let the cache exceed a total of this many bytes
        /// </summary>
        /// <remarks>
        /// Note: this is nearly true.  The current implementation will allow
        /// for a request to exceed this size (and succeed).  On the next
        /// subsequent request though, the overage will be noticed and the
        /// cache cleared before processing the request.
        /// </remarks>
        private const long MaxCachedBytes = 32 * MB;

        /// <summary>
        /// If the range requested is less than this many bytes,
        /// round up to this many bytes for the Azure Storage Get request.
        /// </summary>
        private const long DefaultLength = 4 * MB;

        // The max timeout given to Azure Storage for completing a blob copy.
        // i.e., not a Gridwich request timeout, but one for the Azure async copy request.
        private readonly TimeSpan _blobCopyTimeout = TimeSpan.FromHours(4.0);

        private readonly IBlobBaseClientProvider _blobBaseClientProvider;
        private readonly IBlobContainerClientProvider _blobContainerClientProvider;
        private readonly IAzureStorageManagement _azureStorageManagement;
        private readonly IObjectLogger<StorageService> _log;

        /// <summary>
        /// A cache of byte range data previously retrieved from Azure Storage for
        /// the blob named by LastUriCached.
        /// </summary>
        private readonly RangeTree<int, CachedHttpRangeContent> cachedContentTree = new RangeTree<int, CachedHttpRangeContent>();

        /// <summary>
        /// The URI for which cachedContentTree was holding entries.  When we change Uris
        /// cached content must be removed to avoid cross-contamination.
        /// </summary>
        private string lastUriCached = string.Empty;

        private long totalContentLength = 0;

        /// <summary>
        /// Special value for GetOrDownloadContentAsync length parameter.  If you pass this value
        /// for the desiredSize argument, StorageService.DefaultLength (currently 4MB, above) will be used.
        /// </summary>
        public const long UseDefaultLength = IStorageService.UseDefaultLength;

        /// <summary>
        /// Default Time to Live in minutes.
        /// </summary>
        public const int DefaultTimeToLiveMinutes = IStorageService.DefaultTimeToLiveMinutes;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageService"/> class.
        /// </summary>
        /// <param name="settingsProvider">settingsProvider.</param>
        /// <param name="blobBaseClientProvider">blobBaseClientProvider.</param>
        /// <param name="blobContainerClientProvider">blobContainerClientProvider.</param>
        /// <param name="azureStorageManagement">azureStorageManagement.</param>
        /// <param name="log">log.</param>
        public StorageService(
            IBlobBaseClientProvider blobBaseClientProvider,
            IBlobContainerClientProvider blobContainerClientProvider,
            IAzureStorageManagement azureStorageManagement,
            IObjectLogger<StorageService> log)
        {
            _blobBaseClientProvider = blobBaseClientProvider;
            _blobContainerClientProvider = blobContainerClientProvider;
            _azureStorageManagement = azureStorageManagement;
            _log = log;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            CleanUpCachedContentTree();
        }

        /// <inheritdoc/>
        public string GetSasUrlForBlob(Uri blobUri, TimeSpan ttl, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            try
            {
                var blobUriBuilder = new BlobUriBuilder(blobUri);

                // Create a SAS token that's valid for the TimeSpan, plus a back-off start for clock skew.
                var timeRange = StorageHelpers.CreateTimeRangeForUrl(ttl);
                BlobSasBuilder sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobUriBuilder.BlobContainerName,
                    BlobName = blobUriBuilder.BlobName,
                    Resource = "b", // "b" is for blob
                    StartsOn = timeRange.StartTime,
                    ExpiresOn = timeRange.EndTime,
                }.UnescapeTargetPath(); // Important adjustment(s) for SAS computation

                sasBuilder.SetPermissions(BlobSasPermissions.Read); // read permissions only for the SAS.

                var sleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);
                var userDelegation = sleeve.Service.GetUserDelegationKey(sasBuilder.StartsOn, sasBuilder.ExpiresOn)?.Value;

                if (userDelegation == null)
                {
                    var msg = $@"Unable to get a user delegation key from the Storage service for blob {blobUri}";
                    _log.LogEvent(LogEventIds.StorageServiceMissingConnectionString, msg);
                    throw new GridwichStorageServiceException(blobUri, msg, LogEventIds.StorageServiceMissingConnectionString, context.ClientRequestIdAsJObject);
                }

                var sasToken = sasBuilder.ToSasQueryParameters(userDelegation, blobUriBuilder.AccountName);
                blobUriBuilder.Sas = sasToken;

                // Construct the full URI, including the SAS token. AbsoluteUri (vs. ToString) is to ensure the %HH escaping is used.
                return blobUriBuilder.ToUri().AbsoluteUri;
            }
            catch (RequestFailedException e)
            {
                var msg = $@"Unable to get a user delegation key from the Storage service for blob {blobUri}";
                _log.LogEvent(LogEventIds.StorageServiceMissingConnectionString, msg);
                throw new GridwichStorageServiceException(blobUri, msg, LogEventIds.StorageServiceMissingConnectionString, context.ClientRequestIdAsJObject, e);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToCreateBlobSasUriInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "Failed to generate the SAS url.",
                    LogEventIds.FailedToCreateBlobSasUriInStorageService, context.ClientRequestIdAsJObject, e);
            }
        }

        /// <summary>
        /// Gets the connection string for account.
        /// </summary>
        /// <param name="storageAccountName">Name of the storage account.</param>
        /// <param name="context">The storage context.</param>
        /// <returns>
        /// The connection string to <paramref name="storageAccountName" /></returns>
        /// <remarks>
        /// This method creates a connection string based on the Storage account key.
        /// Since the connection string is not ever sent outside of Griwich, and that
        /// the key itself is obtain from AzureStorageManagement.GetAccountKey, which
        /// in turn, obtains the key information using the MSI identify, this sequence
        /// does not represent any new authorization exposure.
        /// </remarks>
        public string GetConnectionStringForAccount(string storageAccountName, StorageClientProviderContext context)
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(storageAccountName) ?? throw new ArgumentNullException(nameof(storageAccountName));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            var accountKey = _azureStorageManagement.GetAccountKey(storageAccountName);
            if (accountKey == null)
            {
                throw new GridwichStorageServiceException($"Failed to get account key for account named '{storageAccountName}'",
                    LogEventIds.FailedToGetAccountKey, context.ClientRequestIdAsJObject);
            }

            // This one is one long string with many subs, so more readable via StringBuilder than $""
            var connStr = new StringBuilder(150);

            connStr
                .Append("DefaultEndpointsProtocol=").Append(StorageServiceConstants.AzureStorageProtocol)
                .Append(";AccountName=").Append(storageAccountName)
                .Append(";AccountKey=").Append(accountKey)
                .Append(";EndpointSuffix=").Append(StorageServiceConstants.AzureStorageDnsSuffix)
                .Append(";");

            return connStr.ToString();
        }

        /// <inheritdoc/>
        public async Task<BlobProperties> GetBlobPropertiesAsync(Uri blobUri, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            IStorageBlobClientSleeve blobSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);

            BlobProperties blobProperties;
            try
            {
                blobProperties = await blobSleeve.Client.GetPropertiesAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToGetBlobPropertiesInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "Could not get the blob's properties.",
                    LogEventIds.FailedToGetBlobPropertiesInStorageService, context.ClientRequestIdAsJObject, e);
            }

            return blobProperties;
        }

        /// <inheritdoc/>
        public async Task<JObject> GetBlobMetadataAsync(Uri blobUri, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            JObject blobMetadata = null;
            var blobProperties = await GetBlobPropertiesAsync(blobUri, context).ConfigureAwait(false);

            var metadata = blobProperties?.Metadata;

            if (metadata != null)
            {
                // TODO: is this intended? Shouldn't we use JsonHelpers.ConvertFromNative(metadata) instead?
                blobMetadata = JObject.FromObject(metadata);
            }

            return blobMetadata;
        }

        /// <inheritdoc/>
        public async Task<BlobDownloadInfo> DownloadHttpRangeAsync(Uri blobUri, StorageClientProviderContext context, HttpRange httpRange = default)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            // Note: when the httpRange struct is omitted it defaults to 'default' which downloads the entire blob.

            IStorageBlobClientSleeve blobBaseClient = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);

            BlobDownloadInfo blobDownloadInfo;
            try
            {
                blobDownloadInfo = (await blobBaseClient.Client.DownloadAsync(httpRange).ConfigureAwait(false)).Value;
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToDownloadHttpRangeInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "Could not download the HTTP range for a blob.",
                    LogEventIds.FailedToDownloadHttpRangeInStorageService, context.ClientRequestIdAsJObject, e);
            }

            return blobDownloadInfo;
        }

        /// <inheritdoc/>
        public async Task<CopyFromUriOperation> BlobCopy(Uri sourceUri, Uri destinationUri, StorageClientProviderContext context)
        {
            _ = sourceUri ?? throw new ArgumentNullException(nameof(sourceUri));
            _ = destinationUri ?? throw new ArgumentNullException(nameof(destinationUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            IStorageBlobClientSleeve destinationBlobBaseClient = _blobBaseClientProvider.GetBlobClientSleeveForUri(destinationUri, context);
            var containerSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(destinationUri, context);

            // 0. Create Container if missing
            try
            {
                await containerSleeve.Client.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.StorageServiceFailedCreatingTargetContainerForBlobCopy, e, destinationUri);
                throw new GridwichStorageServiceException(destinationUri, "Could not create the destination container.",
                    LogEventIds.StorageServiceFailedCreatingTargetContainerForBlobCopy, context.ClientRequestIdAsJObject, e);
            }

            CopyFromUriOperation blobCopyInfo;
            try
            {
                // 1. Get SAS for Source
                string sasUri = GetSasUrlForBlob(sourceUri, _blobCopyTimeout, context);

                // 2. Copy Async to Dest
                blobCopyInfo = await destinationBlobBaseClient.Client.StartCopyFromUriAsync(new Uri(sasUri)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToCopyBlobInStorageService, e, sourceUri);
                throw new GridwichStorageServiceException(sourceUri, $"Could not copy blob from {sourceUri} to {destinationUri}.",
                    LogEventIds.FailedToCopyBlobInStorageService, context.ClientRequestIdAsJObject, e);
            }

            return blobCopyInfo;
        }

        /// <inheritdoc/>
        public async Task<CachedHttpRangeContent> GetOrDownloadContentAsync(Uri blobUri, long desiredOffset, long desiredSize, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            // fixup for default size.
            if (desiredSize == UseDefaultLength)
            {
                desiredSize = DefaultLength;
            }

            if (desiredOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredOffset), $"Must be greater than zero. {desiredOffset}");
            }
            if (desiredSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredSize), $"Must be greater than zero. {desiredSize}");
            }
            if (desiredSize > MaxCachedBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredSize), $"Must be less than or equal to {MaxCachedBytes}.");
            }

            // Since the cachedContentTree is only managed on a single-URI basis, we need
            // to determine if the cache content is for the requested URI.  If not, flush
            // out the cache as we're starting over for a new URI.
            string uriString = blobUri.ToString();

            if (uriString != lastUriCached)
            {
                CleanUpCachedContentTree();
                lastUriCached = uriString;
            }
            else
            {
                // It's for the same URI as last call, so check cache for ranges that contain this offset.
                var cachedHttpRangeContentEntry = cachedContentTree
                    .Query((int)desiredOffset)
                    .OrderByDescending(e => e.CachedHttpRange.Offset)
                    .FirstOrDefault();

                if (cachedHttpRangeContentEntry != default(CachedHttpRangeContent))
                {
                    // Console.WriteLine($"Found Range:\t\t {cachedHttpRangeContentEntry.Range.Offset},\t\t {cachedHttpRangeContentEntry.Range.Offset + cachedHttpRangeContentEntry.Range.Length - 1}");
                    _log.LogEventObject(LogEventIds.FoundCachedHttpRange, new { httpRange = cachedHttpRangeContentEntry.CachedHttpRange, desiredOffset });
                    return cachedHttpRangeContentEntry;
                }
            }

            // No luck, nothing suitable in the cache so we're going to have to download a new range to cover
            // the request. Clean out the whole tree if we are about to exceed the MaxMemorySize.
            if (totalContentLength + desiredSize >= MaxCachedBytes)
            {
                CleanUpCachedContentTree();
            }

            int downloadedContentLength = 0;
            MemoryStream memStream = null;
            var requestedHttpRange = new HttpRange(desiredOffset, desiredSize);
            try
            {
                using var downloadResponse = await DownloadHttpRangeAsync(blobUri, context, requestedHttpRange).ConfigureAwait(false);
                downloadedContentLength = (int)downloadResponse.ContentLength;
                totalContentLength += downloadedContentLength;

#pragma warning disable CA2000 // Dispose objects before losing scope
                memStream = new MemoryStream(downloadedContentLength);
#pragma warning restore CA2000 // Dispose objects before losing scope

                downloadResponse.Content.CopyTo(memStream);
            }
            catch (Exception e) when (
                e is ArgumentOutOfRangeException ||
                e is ArgumentNullException ||
                e is NotSupportedException ||
                e is ObjectDisposedException ||
                e is IOException)
            {
                _log.LogExceptionObject(LogEventIds.FailedToDownloadContentInStorageService, e, new { blobUri, httpRange = requestedHttpRange });
                throw new GridwichStorageServiceException(blobUri, "Could not download content for a blob.",
                    LogEventIds.FailedToDownloadContentInStorageService, context.ClientRequestIdAsJObject, e);
            }

            var actualHttpRange = new HttpRange(desiredOffset, downloadedContentLength);
            var cachedHttpRangeContent = new CachedHttpRangeContent(actualHttpRange, memStream);
            cachedContentTree.Add((int)actualHttpRange.Offset, (int)(actualHttpRange.Offset + actualHttpRange.Length - 1), cachedHttpRangeContent);

            // Console.WriteLine($"Added Range:\t\t {actualHttpRange.Offset},\t\t {actualHttpRange.Offset + actualHttpRange.Length - 1}");
            _log.LogEventObject(LogEventIds.HttpRangeDownloadedFinished, new { httpRange = actualHttpRange, desiredOffset });

            return cachedHttpRangeContent;
        }

        /// <inheritdoc/>
        public async Task<bool> ChangeBlobTierAsync(Uri blobUri, BlobAccessTier accessTier, BlobRehydratePriority rehydratePriority, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = accessTier ?? throw new ArgumentNullException(nameof(accessTier));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            IStorageBlobClientSleeve blobSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);

            Response response;
            try
            {
                response = await blobSleeve.Client.SetAccessTierAsync(accessTier.ToAzureEnum(), null, rehydratePriority.ToAzureEnum()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToChangeTierInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "Could not change tier of the blob.",
                    LogEventIds.FailedToChangeTierInStorageService, context.ClientRequestIdAsJObject, e);
            }

            if (response.Status == 200)
            {
                return true;
            }

            _log.LogEventObject(LogEventIds.ChangeBlobTierUnexpectedResponse, new { blobUri, response });
            throw new GridwichStorageServiceException(blobUri, $"Unexpected response when changing the blob's tier: {response}",
                LogEventIds.ChangeBlobTierUnexpectedResponse, context.ClientRequestIdAsJObject);
        }

        /// <inheritdoc/>
        public async Task<bool> ContainerCreateAsync(string storageAccountName, string containerName, StorageClientProviderContext context)
        {
            _ = StringHelpers.NullIfNullOrEmpty(storageAccountName) ?? throw new ArgumentNullException(nameof(storageAccountName));
            _ = StringHelpers.NullIfNullOrEmpty(containerName) ?? throw new ArgumentNullException(nameof(containerName));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            BlobContainerInfo blobContainerInfo;
            var containerUri = StorageHelpers.BuildBlobStorageUri(storageAccountName, containerName);
            try
            {
                var containerSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(containerUri, context);
                blobContainerInfo = await containerSleeve.Client.CreateAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new GridwichStorageServiceException(containerUri, "Failed to create the container.",
                    LogEventIds.FailedToCreateContainer, context.ClientRequestIdAsJObject, e);
            }

            if (blobContainerInfo != null)
            {
                _log.LogEventObject(LogEventIds.ContainerCreateProgress, blobContainerInfo);
                return true;
            }

            throw new GridwichStorageServiceException(containerUri, "Failed to create the container.",
                LogEventIds.FailedToCreateContainer, context.ClientRequestIdAsJObject);
        }

        /// <inheritdoc/>
        public async Task<bool> ContainerDeleteAsync(string storageAccountName, string containerName, StorageClientProviderContext context)
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(storageAccountName) ?? throw new ArgumentNullException(nameof(storageAccountName));
            _ = StringHelpers.NullIfNullOrWhiteSpace(containerName) ?? throw new ArgumentNullException(nameof(containerName));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            Response response;
            var containerUri = StorageHelpers.BuildBlobStorageUri(storageAccountName, containerName);
            try
            {
                var containerSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(containerUri, context);
                response = await containerSleeve.Client.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToDeleteContainer, e, containerUri);
                throw new GridwichStorageServiceException(containerUri, "Could not delete blob container.",
                    LogEventIds.FailedToDeleteContainer, context.ClientRequestIdAsJObject, e);
            }

            if (response.Status == 200)
            {
                return true;
            }

            _log.LogEventObject(LogEventIds.DeleteContainerUnexpectedResponse, new { containerUri, response });
            throw new GridwichStorageServiceException(containerUri, $"Unexpected response while deleting blob container: {response.Status} {response.ReasonPhrase}",
                LogEventIds.DeleteContainerUnexpectedResponse, context.ClientRequestIdAsJObject);
        }

        /// <inheritdoc/>
        public async Task ContainerSetPublicAccessAsync(string accountName, string containerName, ContainerAccessType accessType, StorageClientProviderContext context)
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(accountName) ?? throw new ArgumentNullException(nameof(accountName));
            _ = StringHelpers.NullIfNullOrWhiteSpace(containerName) ?? throw new ArgumentNullException(nameof(containerName));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            BlobContainerInfo containerInfo;
            try
            {
                var connectionString = GetConnectionStringForAccount(accountName, context);
                var containerSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForConnectionString(connectionString, containerName, context);
                containerInfo = await containerSleeve.Client.SetAccessPolicyAsync(accessType.ToAzureEnum()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToChangeContainerAccess, e, new { accountName, containerName });
                throw new GridwichStorageServiceException("Could not change blob container access.",
                    LogEventIds.FailedToChangeContainerAccess, context.ClientRequestIdAsJObject, e);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetBlobMetadataAsync(Uri blobUri, IDictionary<string, string> metadata, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));
            if (metadata is null || metadata.Count == 0)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            bool exists = await GetBlobExistsAsync(blobUri, context).ConfigureAwait(false);

            if (!exists)
            {
                _log.LogEventObject(LogEventIds.StorageServiceAttemptToAddMetadataToBlobThatDoesNotExist, blobUri);
                throw new GridwichStorageServiceException(blobUri, "The operation failed because the requested blob does not exist.",
                    LogEventIds.StorageServiceAttemptToAddMetadataToBlobThatDoesNotExist, context.ClientRequestIdAsJObject);
            }

            IStorageBlobClientSleeve blobSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);

            try
            {
                await blobSleeve.Client.SetMetadataAsync(metadata).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToSetMetadataInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "Could not set the metadata of the blob.",
                    LogEventIds.FailedToSetMetadataInStorageService, context.ClientRequestIdAsJObject, e);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> GetBlobExistsAsync(Uri blobUri, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            IStorageBlobClientSleeve blobSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);

            try
            {
                return await blobSleeve.Client.ExistsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToCheckBlobExistenceDueToStorageExceptionInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "The operation failed because the requested blob does not exist.",
                    LogEventIds.FailedToCheckBlobExistenceDueToStorageExceptionInStorageService, context.ClientRequestIdAsJObject, e);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<BlobItem>> ListBlobsAsync(Uri containerUri, StorageClientProviderContext context)
        {
            _ = containerUri ?? throw new ArgumentNullException(nameof(containerUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            var containerSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(containerUri, context);
            var blobList = new List<BlobItem>();

            try
            {
                await foreach (var blobItem in containerSleeve.Client.GetBlobsAsync(BlobTraits.Metadata))
                {
                    blobList.Add(blobItem);
                }
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.FailedToListBlobsDueToStorageExceptionInStorageService, e, containerUri);
                throw new GridwichStorageServiceException(containerUri, "Could not list the blobs in the container.",
                    LogEventIds.FailedToListBlobsDueToStorageExceptionInStorageService, context.ClientRequestIdAsJObject, e);
            }

            return blobList;
        }

        /// <inheritdoc/>
        public async Task<bool> BlobDelete(Uri blobUri, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            // 0. Get the SDK storage client
            IStorageBlobClientSleeve blobSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(blobUri, context);

            Response<bool> resp;
            try
            {
                // 1. Get a SAS for the blob
                string sasUri = GetSasUrlForBlob(blobUri, new TimeSpan(0, 5, 0), context);

                // 2. Request the deletion
                // From the SDK source (see below), there are 3 possible responses:
                //    1. true -- blob existed and was deleted from index.
                //    2. false -- blob did not exist
                //    3a. RequestFailedException -- some storage problem, other than the blob not existing.
                //    3b. Some other exception -- unlikely but possible.
                // Azure SDK Source for Delete: https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/storage/Azure.Storage.Blobs/src/BlobBaseClient.cs
                // DeleteBlob REST Call: https://docs.microsoft.com/en-us/rest/api/storageservices/delete-blob
                resp = await blobSleeve.Client.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // something other than an expected Storage problem.
                _log.LogExceptionObject(LogEventIds.FailedToDeleteDueToStorageExceptionInStorageService, e, blobUri);
                throw new GridwichStorageServiceException(blobUri, "Failed to delete the blob.",
                    LogEventIds.FailedToDeleteDueToStorageExceptionInStorageService, context.ClientRequestIdAsJObject, e);
            }

            return resp;
        }

        /// <summary>
        /// Removes and disposes of the contents of the cachedContentTree.
        /// </summary>
        private void CleanUpCachedContentTree()
        {
            if (cachedContentTree.Any())
            {
                foreach (var rangeValuePair in cachedContentTree)
                {
                    cachedContentTree.Remove(rangeValuePair.Value);
                    rangeValuePair.Value.Dispose();
                }
            }
            totalContentLength = 0;
        }

        /// <summary>Correctly concatenate containerUri and blobName to form a storage URI</summary>
        /// <param name="containerUri">The blob's storage URI, including the container name</param>
        /// <param name="blobName">The blob's name, with path. (i.e. everything after the container name)</param>
        /// <returns>The concatenated URI or throws an Argument exception if the parts were malformed.
        /// e.g. not including a container name at the end of containerUri.
        /// <returns>
        /// <example>
        /// e.g.  BlobUriBuilder("https://xxx.blob.core.windows.net/containername", "filepath/filename.mp4")
        ///                =>    "https://xxx.blob.core.windows.net/containername/filepath/filename.mp4"
        /// </example>
        /// </remarks>
        /// This function is mainly about enforcing https and avoiding having mainline code checking for
        /// oddities like whether that container portion has a trailing '/' but the blobName doesn't, etc.
        ///
        /// It was intentional not to "paper-over" transgressions like missing the container on containerUri
        /// (by adding the invisible $root container if needed).  Intent is to instead fail fast.
        /// </remarks>
        [method: SuppressMessage(
            "Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings",
            Justification = "Ill-advised... if heeded, results in new complaints leading to not handling errors herein")]
        public Uri CreateBlobUrl(string containerUri, string blobName)
        {
            _ = StringHelpers.NullIfNullOrWhiteSpace(containerUri) ?? throw new ArgumentException("containerUri is invalid", nameof(containerUri));
            _ = StringHelpers.NullIfNullOrWhiteSpace(blobName) ?? throw new ArgumentException("blobName is invalid", nameof(blobName));

            var blobUriBuilder = new BlobUriBuilder(new Uri(containerUri.TrimEnd('/')));
            if (string.IsNullOrEmpty(blobUriBuilder.BlobContainerName))
            {
                // Malformed URL - no container name -- log it & throw the exception.
                string msg = "Container Uri does not include a container name." +
                             $"containerUri='{containerUri}', blobName='{blobName}'";

                _log.LogEvent(LogEventIds.StorageServiceFailingUriMissingContainer, msg);
                throw new ArgumentException(msg, nameof(containerUri));
            }

            blobUriBuilder.Scheme = "https"; // force https, even if others forget.
            blobUriBuilder.BlobName = blobName.TrimStart('/');

            return blobUriBuilder.ToUri();
        }
    }
}
