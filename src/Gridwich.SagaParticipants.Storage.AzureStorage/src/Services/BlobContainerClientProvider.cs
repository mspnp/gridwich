using Azure.Core;
using Azure.Storage.Blobs;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using LazyCache;
using System;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Services
{
    /// <summary>
    /// Provides a BlobContainerClient as an application-wide singleton.
    /// </summary>
    public class BlobContainerClientProvider : IBlobContainerClientProvider
    {
        // TODO: left at 10 minutes to facilitate debugging, but for Production, should be set to < 1 minute (duration of a request)
        private static readonly TimeSpan ClientExpirationTime = new TimeSpan(0, 10, 0);
        private static readonly string CacheKeyPrefix = $"{typeof(StorageService)}-C-{nameof(GetBlobContainerSleeveForUri)}";

        private readonly ISettingsProvider _settingsProvider;
        private readonly IAppCache _blobContainerClientCache;
        private readonly IObjectLogger<BlobContainerClientProvider> _log;
        private readonly TokenCredential _identity;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobContainerClientProvider" /> class.
        /// </summary>
        /// <param name="settingsProvider">settingsProvider.</param>
        /// <param name="appCache">appCache.</param>
        /// <param name="log">log.</param>
        /// <param name="identity">The identity.</param>
        public BlobContainerClientProvider(
            ISettingsProvider settingsProvider,
            IAppCache appCache,
            IObjectLogger<BlobContainerClientProvider> log,
            TokenCredential identity)
        {
            _settingsProvider = settingsProvider;
            _blobContainerClientCache = appCache;
            _log = log;
            _identity = identity;
        }

        /// <inheritdoc/>
        public IStorageContainerClientSleeve GetBlobContainerSleeveForUri(Uri uri, StorageClientProviderContext context)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context),
                    $"Use {nameof(StorageClientProviderContext.None)} instead of null for 'empty' context");
            }

            // Cache policy notes -- see commentary in Gridwich.Core/Models/BlobBaseClientProvider.cs

            var cacheKey = GetCacheKeyForBlobContainerClient(uri, context);
            var sleeve = _blobContainerClientCache.Get<IStorageContainerClientSleeve>(cacheKey);

            if (sleeve != null)
            {
                // found one, fix up it's context values to the new ones.
                // Note that the fact that we found one means the ClientRequestID is already correct, but
                // we should update the ETag & flag information, thus the ResetTo().
                sleeve.Context.ResetTo(context);
            }
            else
            {
                sleeve = CreateBlobContainerClientForUri(uri, context);
                _blobContainerClientCache.Add(cacheKey, sleeve, ClientExpirationTime);
            }

            return sleeve;
        }

        /// <inheritdoc/>
        public IStorageContainerClientSleeve GetBlobContainerSleeveForConnectionString(string connectionString, string containerName, StorageClientProviderContext context)
        {
            _ = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _ = containerName ?? throw new ArgumentNullException(nameof(containerName));
            _ = context ?? throw new ArgumentNullException(nameof(context));

            var cacheKey = GetCacheKeyForBlobContainerClient(connectionString, containerName, context);
            var sleeve = _blobContainerClientCache.Get<StorageContainerClientSleeve>(cacheKey);

            if (sleeve != null)
            {
                sleeve.Context.ResetTo(context);
            }
            else
            {
                sleeve = CreateBlobContainerClientForConnectionString(connectionString, containerName, context);
                _blobContainerClientCache.Add(cacheKey, sleeve, ClientExpirationTime);
            }

            return sleeve;
        }

        /// <summary>
        /// Returns unique cache key.
        /// </summary>
        /// <param name="uri">the container URI</param>
        /// <param name="context">current storage context</param>
        /// <returns>A unique cache key based on uri and context.</returns>
        internal static string GetCacheKeyForBlobContainerClient(Uri uri, StorageClientProviderContext context)
        {
            return $"{CacheKeyPrefix}-{uri.GetHashCode()}-{context.ClientRequestID.GetHashCode()}";
        }

        /// <summary>
        /// Returns unique cache key.
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="containerName">Container name</param>
        /// <param name="context">current storage context</param>
        /// <returns>A unique cache key based on the connection string and container name.</returns>
        internal static string GetCacheKeyForBlobContainerClient(string connectionString, string containerName, StorageClientProviderContext context)
        {
            return $"{CacheKeyPrefix}-{connectionString.GetHashCode()}-{containerName}-{context.ClientRequestID.GetHashCode()}";
        }

        /// <summary>
        /// Creates a BlobContainerClient from a connection string. Called when one does not exist yet.
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <param name="containerName">Container name</param>
        /// <param name="context">current storage context</param>
        /// <returns>
        /// A BlobContainerClient object.
        /// </returns>
        internal StorageContainerClientSleeve CreateBlobContainerClientForConnectionString(string connectionString, string containerName, StorageClientProviderContext context)
        {
            var ctxCopy = new StorageClientProviderContext(context);

            BlobContainerClient blobContainerClient;
            BlobClientOptions clientOptions;
            try
            {
                clientOptions = new BlobClientOptions();
                var clientRequestIdPolicy = new BlobClientPipelinePolicy(ctxCopy);
                blobContainerClient = new BlobContainerClient(connectionString, containerName, clientOptions);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.BlobContainerClientProviderFailedToCreateBlobContainerClient, e, new { connectionString, containerName });
                throw;
            }

            return new StorageContainerClientSleeve(blobContainerClient,
                new BlobServiceClient(connectionString, clientOptions),
                ctxCopy);
        }

        /// <summary>
        /// Creates a BlobContainerClient.  Called when one does not exist yet.
        /// </summary>
        /// <param name="uri">The target Uri.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A BlobContainerClient object.
        /// </returns>
        internal StorageContainerClientSleeve CreateBlobContainerClientForUri(
                    Uri uri,
                    StorageClientProviderContext context)
        {
            // set up a copy of the Context...
            var ctxCopy = new StorageClientProviderContext(context);

            BlobContainerClient blobContainerClient;
            BlobUriBuilder containerUriBuilder;
            BlobClientOptions clientOptions;
            try
            {
                containerUriBuilder = new BlobUriBuilder(uri);
                clientOptions = new BlobClientOptions();
                var clientRequestIdPolicy = new BlobClientPipelinePolicy(ctxCopy);
                var uriC = StorageHelpers.BuildBlobStorageUri(containerUriBuilder.AccountName, containerUriBuilder.BlobContainerName);
                blobContainerClient = new BlobContainerClient(uriC, _identity, clientOptions);
            }
            catch (Exception fe) when (fe is ArgumentNullException || fe is UriFormatException)
            {
                var aex = new ArgumentException(LogEventIds.BlobContainerClientProviderUriMissingAccountName.Name, fe);
                _log.LogExceptionObject(LogEventIds.BlobContainerClientProviderUriMissingAccountName, aex, uri);
                throw aex;
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.BlobContainerClientProviderFailedToCreateBlobContainerClient, e, uri);
                throw;
            }


            try
            {
                var accountUri = StorageHelpers.BuildStorageAccountUri(containerUriBuilder.AccountName, buildForBlobService: true);
                var sleeve = new StorageContainerClientSleeve(blobContainerClient,
                    new BlobServiceClient(accountUri, _identity, clientOptions),
                    ctxCopy);
                return sleeve;
            }
            catch (ArgumentException aex)
            {
                _log.LogExceptionObject(LogEventIds.BlobContainerClientProviderUriMissingAccountName, aex, uri);
                throw;
            }
        }

        /// <summary>Reset this instance to reflect new context information.</summary>
        /// <param name="bcs">The sleeve to have context reset</param>
        /// <param name="ctx">The new context from which to update the sleeve's context.
        /// Note that the context state is copied to the sleeve, not used in place from ctx.</param>
        /// <returns>A reference to the input bcs parameter, updated to reflect the new context.</returns>
        internal static StorageContainerClientSleeve ResetSleeve(StorageContainerClientSleeve bcs, StorageClientProviderContext ctx)
        {
            // important to update the existing context in-place as policy references a particular instance
            bcs.Context.ResetTo(ctx);
            return bcs;
        }
    }
}
