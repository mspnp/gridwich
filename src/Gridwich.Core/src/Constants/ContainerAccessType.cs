using Azure.Storage.Blobs.Models;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// Wrapper enum for Azure's PublicAccessType used to avoid leaking any possible new values to Requestor.
    /// </summary>
    public enum ContainerAccessType
    {
        /// <summary>
        /// Gets Blob.
        /// </summary>
        Blob = PublicAccessType.Blob,

        /// <summary>
        /// Gets BlobContainer.
        /// </summary>
        BlobContainer = PublicAccessType.BlobContainer,

        /// <summary>
        /// Gets None.
        /// </summary>
        None = PublicAccessType.None,
    }
}