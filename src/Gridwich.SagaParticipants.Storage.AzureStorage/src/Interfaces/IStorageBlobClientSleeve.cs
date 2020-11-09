using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Gridwich.Core.Models;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces
{
    /// <summary>
    /// A composite type to contain the client/context pair as returned from the
    /// BlobBaseClientProvider.
    /// </summary>
    /// <remarks>
    /// This type is needed (vs. simply having the provider return a "bare" client
    /// instance) because of the need to keep the clientRequestID/ETag values in
    /// sync between the consumer and the policy buried within the BlobBaseClient.
    /// </remarks>
    public interface IStorageBlobClientSleeve
    {
        /// <summary>Gets the BlobBaseClient for this sleeve.</summary>
        BlobBaseClient Client { get; }

        /// <summary>Gets the blob service for this blob.</summary>
        BlobServiceClient Service { get; }

        /// <summary>Gets the Storage context object.</summary>
        StorageClientProviderContext Context { get; }
    }
}
