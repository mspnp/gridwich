using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using LazyCache;
using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gridwich.SagaParticipants.Storage.AzureStorageTests")]

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Services
{
    /// <summary>
    /// Provides instances of Azure SDK BlobBaseClient via an application-wide singleton.
    /// </summary>
    public class BlobBaseClientProvider : IBlobBaseClientProvider
    {
        // TODO: left at 10 minutes to facilitate debugging, but for Production, should be set to < 1 minute (duration of a request)
        private static readonly TimeSpan ClientExpirationTime = new TimeSpan(0, 10, 0);
        private static readonly string CacheKeyPrefix = $"{typeof(StorageService)}-B-{nameof(GetBlobClientSleeveForUri)}";

        private readonly ISettingsProvider _settingsProvider;
        private readonly IAppCache _blobBaseClientCache;
        private readonly IObjectLogger<BlobBaseClientProvider> _log;
        private readonly TokenCredential _identity;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobBaseClientProvider" /> class.
        /// </summary>
        /// <param name="settingsProvider">settingsProvider.</param>
        /// <param name="appCache">appCache.</param>
        /// <param name="log">log.</param>
        /// <param name="identity">The identity.</param>
        public BlobBaseClientProvider(
            ISettingsProvider settingsProvider,
            IAppCache appCache,
            IObjectLogger<BlobBaseClientProvider> log,
            TokenCredential identity)
        {
            _settingsProvider = settingsProvider;
            _blobBaseClientCache = appCache;
            _log = log;
            _identity = identity;
        }

        /// <inheritdoc/>
        public IStorageBlobClientSleeve GetBlobClientSleeveForUri(Uri blobUri, StorageClientProviderContext context)
        {
            if (blobUri == null)
            {
                throw new ArgumentNullException(nameof(blobUri));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Cache policy
            //
            // We need to support sequences of related storage operations.  For example, in Gridwich,
            // deleting a blob requires two sequential steps: retrieving metadata and then deleting the blob. If
            // we permit caching keyed solely on blob URI, there arise issues with "crossing the streams" -- i.e.,
            // two request streams both happen to target the same blob at the overlapping times.  Specifically,
            // retrieving a client primed for a different Operation Context/ETag combination causes failures.
            //
            // Another consideration is that the Storage Service, the consumer of the cache items, is not
            // structured to pass clients from method to method.   In general, if one storage service method
            // calls another (e.g., DeleteBlob calls GetMetadata), it passed only the URI and context, not the client.
            // Thus it is important that it be possible for those two to be sufficient for each method to retrieve
            // a "hot" client instance from the cache (or maybe a fresh one, the first time), avoiding the need for
            // using a fresh client each time and incurring authentication, connection establishment, etc.
            //
            // For those reasons, two policies/conventions:
            //    1. The client cache will always be keyed solely on the blobURI/Context combination.
            //       Should some valid circumstance arise where a context is not available, code falls back to using
            //       StorageClientProviderContext.NO_CONTEXT.
            //    2. The cache item expiry time will be dropped to a much lower time span than the original 10 minutes.
            //       Cache items should persist for as close to the duration of a request.  Since Storage service
            //       methods are unaware of how callers are sequencing them, it is not clear when it is "safe" for
            //       a method to remove cache items.  Thus, we will have to depend on cache item expiry to keep
            //       the cache size down.

            var cacheKey = GetCacheKeyForBlobBaseClient(blobUri, context);
            var sleeve = _blobBaseClientCache.Get<IStorageBlobClientSleeve>(cacheKey);

            if (sleeve != null)
            {
                // found one, fix up it's context values to the new ones.
                // Note that the fact that we found one means the ClientRequestID is already correct, but
                // we should update the ETag & flag information, thus the ResetTo().
                sleeve.Context.ResetTo(context);
            }
            else
            {
                sleeve = CreateBlobClientSleeveForUri(blobUri, context);
                _blobBaseClientCache.Add(cacheKey, sleeve, ClientExpirationTime);
            }

            return sleeve;
        }

        /// <summary>
        /// Returns unique cache key.
        /// </summary>
        /// <param name="blobUri">blobUri.</param>
        /// <returns>A unique cache key.</returns>
        internal static string GetCacheKeyForBlobBaseClient(Uri blobUri)
        {
            return GetCacheKeyForBlobBaseClient(blobUri, StorageClientProviderContext.None);
        }

        /// <summary>
        /// Returns unique cache key.
        /// </summary>
        /// <param name="blobUri">blobUri.</param>
        /// <param name="context">the storage context (containing clientRequestId)</param>
        /// <returns>A unique cache key.</returns>
        internal static string GetCacheKeyForBlobBaseClient(Uri blobUri, StorageClientProviderContext context)
        {
            if (context == null)
            {
                context = StorageClientProviderContext.None;
            }

            return $"{CacheKeyPrefix}-{blobUri.GetHashCode()}-{context.ClientRequestID.GetHashCode()}";
        }

        /// <summary>
        /// Creates a BlobClientSleeve (BlobBaseClient + Context).  Called when one does not exist yet.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="ctx">The context.</param>
        /// <returns>
        /// A BlobBaseClient object.
        /// </returns>
        internal StorageBlobClientSleeve CreateBlobClientSleeveForUri(Uri blobUri, StorageClientProviderContext ctx)
        {
            BlobBaseClient blobBaseClient;
            BlobUriBuilder blobUriBuilder;

            var contextCopy = new StorageClientProviderContext(ctx);
            var blobClientOptions = new BlobClientOptions();

            try
            {
                blobUriBuilder = new BlobUriBuilder(blobUri);
                var clientRequestIdPolicy = new BlobClientPipelinePolicy(contextCopy);
                blobClientOptions.AddPolicy(clientRequestIdPolicy, HttpPipelinePosition.PerCall);
                blobBaseClient = new BlobBaseClient(blobUri, _identity, blobClientOptions);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.BlobBaseClientProviderFailedToCreateBlobBaseClient, e, blobUri);
                throw;
            }

            try
            {
                // BlobUriBuilder SDK class will just give null for Accountname if URL is malformed, so let flow upwards
                _ = StringHelpers.NullIfNullOrWhiteSpace(blobUriBuilder.AccountName) ?? throw new UriFormatException($@"Malformed Azure Storage URL: {blobUri}");

                var accountUri = StorageHelpers.BuildStorageAccountUri(blobUriBuilder.AccountName, buildForBlobService: true);
                var newSleeve = new StorageBlobClientSleeve(blobBaseClient,
                    new BlobServiceClient(accountUri, _identity, blobClientOptions),
                    contextCopy);
                return newSleeve;
            }
            catch (UriFormatException uriex)
            {
                _log.LogExceptionObject(LogEventIds.BlobBaseClientProviderUriMissingAccountName, uriex, blobUri);
                throw new ArgumentException(LogEventIds.BlobBaseClientProviderUriMissingAccountName.Name, nameof(blobUri), uriex);
            }
            catch (ArgumentException aex)
            {
                _log.LogExceptionObject(LogEventIds.BlobBaseClientProviderUriMissingAccountName, aex, blobUri);
                throw;
            }
        }
    }
}
