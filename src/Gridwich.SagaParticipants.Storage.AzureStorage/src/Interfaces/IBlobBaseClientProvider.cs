using System;
using Gridwich.Core.Models;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces
{
    /// <summary>
    /// Provides BlobBaseClient Sleeve instances via an application-wide singleton.
    /// </summary>
    public interface IBlobBaseClientProvider
    {
        /// <summary>
        /// Gets a BlobClientSleeve for the target Uri and context combination.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="context">The storage client context.</param>
        /// <returns>A BlobClientSleeve for the target Uri.</returns>
        public IStorageBlobClientSleeve GetBlobClientSleeveForUri(Uri blobUri, StorageClientProviderContext context);
    }
}