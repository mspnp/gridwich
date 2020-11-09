using System;
using Gridwich.Core.Models;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces
{
    /// <summary>
    /// Provides a BlobContainerClient as an application-wide singleton.
    /// </summary>
    public interface IBlobContainerClientProvider
    {
        /// <summary>Gets a container sleeve (client+context) for the target Uri</summary>
        /// <param name="uri">The target Uri of the container.</param>
        /// <param name="context">Context of the request in process</param>
        /// <returns>The container sleeve for the target Uri.</returns>
        public IStorageContainerClientSleeve GetBlobContainerSleeveForUri(Uri uri, StorageClientProviderContext context);

        /// <summary>Gets a container sleeve (client+context) for the target connectionString and containerName</summary>
        /// <param name="connectionString">The target connectionString.</param>
        /// <param name="containerName">The target containerName.</param>
        /// <param name="context">Context of the request in process</param>
        /// <returns>The container sleeve for the target Uri.</returns>
        public IStorageContainerClientSleeve GetBlobContainerSleeveForConnectionString(string connectionString, string containerName, StorageClientProviderContext context);
    }
}
