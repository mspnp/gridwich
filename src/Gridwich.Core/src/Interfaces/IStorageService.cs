using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.Models;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Interfaces
{
    /// <summary>
    /// Provides various storage access methods.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Adds a SAS token to the Uri which allows for reading the file for the specified time period.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="ttl">A TimeSpan indicating the time to live for the resulting Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>Full http URL, including the generated SAS token.</returns>
        string GetSasUrlForBlob(Uri blobUri, TimeSpan ttl, StorageClientProviderContext context);

        /// <summary>
        /// Get a BlobProperties object, which includes information such as contentLength.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>The BlobProperties object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when blobUri is null.</exception>
        Task<BlobProperties> GetBlobPropertiesAsync(Uri blobUri, StorageClientProviderContext context);

        /// <summary>
        /// Gets a JObject dictionary of key-value pairs that are on the blob as metadata.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>The metadata.</returns>
        Task<JObject> GetBlobMetadataAsync(Uri blobUri, StorageClientProviderContext context);

        /// <summary>
        /// Downloads a byte-range.
        /// Users should call BlobDownloadInfo.Dispose when done reading/copying the data.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <param name="httpRange">Optional httpRange, if omitted 'default' is used to download all. Optimal is 4MB.</param>
        /// <returns>The details and Content returned from downloading a blob.</returns>
        Task<BlobDownloadInfo> DownloadHttpRangeAsync(Uri blobUri, StorageClientProviderContext context, HttpRange httpRange = default);

        /// <summary>
        /// Copies a Blob.
        /// Users should call BlobDownloadInfo.Dispose when done reading/copying the data.
        /// </summary>
        /// <param name="sourceUri">The blobUri to read.</param>
        /// <param name="destinationUri">The target Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <exception cref="Gridwich.StorageServiceException">If any of the steps of the operation fails.</exception>
        /// <returns>The details and Content returned from copying a blob.</returns>
        Task<CopyFromUriOperation> BlobCopy(Uri sourceUri, Uri destinationUri, StorageClientProviderContext context);

        /// <summary>
        /// Delete a Blob.
        /// </summary>
        /// <param name="blobUri">The simple Uri for the blob to be deleted. "Simple" means the non-SAS URI.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>True if the specified blob is no longer present. False there was a failure performing the deletion.</returns>
        /// <remarks>
        /// Note that True covers both the case where an existing blob was deleted, as well as the specified blob was
        /// not found (e.g., possibly already deleted).  False is indicative of an actual failure - bad URI, unknown storage account,
        /// unknown container, etc.
        /// </remarks>
        Task<bool> BlobDelete(Uri blobUri, StorageClientProviderContext context);

        /// <summary>
        /// Special value for GetOrDownloadContentAsync length parameter.  If you pass this value
        /// for the desiredSize argument, the concrete StorageService instance will use it's default
        /// length (currently 4MB for Gridwich.SagaParticipants.Storage).
        /// </summary>
        public const long UseDefaultLength = StorageServiceConstants.UseDefaultBufferSize;

        /// <summary>
        /// Use this to read and seek within a large file.  This function downloads a byte-range
        /// (which is chunk-size-optimized for AzureStorage) to the client, and allows the caller
        /// to read that MemoryStream.
        /// Further calls will not dispose of the already downloaded data, until a memory threshold
        /// is exceeded. This will reduce network calls for seeks back into the data.
        /// NOTE: The desiredOffset MAY NOT be at the beginning of the MemoryStream.
        /// NOTE: The caller should not dispose of the memory stream.
        /// NOTE: This will cache-miss and be no better than range-requests for a sequence of
        /// backward seeks less than DownloadByteBufferSize.  Use DownloadHttpRangeAsync if this is your use case.
        /// </summary>
        /// <param name="blobUri">The blobUri to read.</param>
        /// <param name="desiredOffset">The desired offset that must be within the stream.</param>
        /// <param name="desiredSize">The desired size of the byte range.
        /// For efficiency, passing IStorageService.UseDefaultLength is recommended</param>
        /// <param name="context">The current request context.</param>
        /// <returns>
        /// Provides a MemoryStream containing the desiredOffset, and an HttpRange which
        /// describes the actual starting offset and buffer length of the MemoryStream relative
        /// to the blobUri.
        /// </returns>
        Task<CachedHttpRangeContent> GetOrDownloadContentAsync(
                        Uri blobUri,
                        long desiredOffset,
                        long desiredSize,
                        StorageClientProviderContext context);

        /// <summary>
        /// Adds metadata to a blob.
        /// </summary>
        /// <param name="blobUri">blobUri to get Blob instance for.</param>
        /// <param name="metadata">metadata to add to the Blob.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>bool indicating success or failure.</returns>
        Task<bool> SetBlobMetadataAsync(Uri blobUri, IDictionary<string, string> metadata, StorageClientProviderContext context);

        /// <summary>
        /// Changes the blob's access tier.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="accessTier">The new tier that the blob will have.</param>
        /// <param name="rehydratePriority">The priority to apply to the operation.
        /// Normally, the best choice is <see cref="BlobRehydratePriority.Default"/>, which is the Gridwich-wide default.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>A boolean indicating if the operation succeeded.</returns>
        /// <exception cref="ArgumentNullException">Thrown when blobUri is null.</exception>
        Task<bool> ChangeBlobTierAsync(
                Uri blobUri,
                BlobAccessTier accessTier,
                BlobRehydratePriority rehydratePriority,
                StorageClientProviderContext context);

        /// <summary>
        /// Create the blob's container.
        /// </summary>
        /// <param name="storageAccountName">The storage account where this blob container will be created in.</param>
        /// <param name="containerName">The name of the blob container that will be created</param>
        /// <param name="context">The current request context.</param>
        /// <returns>A boolean indicating if the operation succeeded.</returns>
        /// <exception cref="ArgumentNullException">Thrown when storageAccountName or containerName is null.</exception>
        Task<bool> ContainerCreateAsync(
                string storageAccountName,
                string containerName,
                StorageClientProviderContext context);

        /// <summary>
        /// Delete a blob container.
        /// </summary>
        /// <param name="storageAccountName">The storage account where the container resides</param>
        /// <param name="containerName">The name of the container that will be deleted</param>
        /// <param name="context">The current request context.</param>
        /// <returns>A boolean indicating if the operation succeeded.</returns>
        /// <exception cref="ArgumentNullException">Thrown when storageAccountName or containerName is null.</exception>
        Task<bool> ContainerDeleteAsync(string storageAccountName, string containerName, StorageClientProviderContext context);

        /// <summary>
        /// Set container public access.
        /// </summary>
        /// <param name="storageAccountName">The storage account where the container resides</param>
        /// <param name="containerName">The name of the container</param>
        /// <param name="accessType">New access type for the container</param>
        /// <param name="context">The current request context.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the arguments is null/invalid.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ContainerSetPublicAccessAsync(
            string storageAccountName,
            string containerName,
            ContainerAccessType accessType,
            StorageClientProviderContext context);

        /// <summary>
        /// Safely creates a full blob Uri from a container name and blobName.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <returns>A fully formed Uri.</returns>
        Uri CreateBlobUrl(string containerName, string blobName);

        /// <summary>
        /// Check if a blob exists.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>True if the blob specified in the Uri exists. False otherwise.</returns>
        Task<bool> GetBlobExistsAsync(Uri blobUri, StorageClientProviderContext context);

        /// <summary>
        /// Gets a list of all the elements in a storage account's container.
        /// </summary>
        /// <param name="containerUri">The target Uri.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>The list of items in the container, including items in subfolders.</returns>
        Task<IEnumerable<BlobItem>> ListBlobsAsync(Uri containerUri, StorageClientProviderContext context);

        /// <summary>
        /// Default Time to Live (ttl) in minutes.
        /// </summary>
        public const int DefaultTimeToLiveMinutes = StorageServiceConstants.DefaultTimeToLiveMinutes;

        /// <summary>
        /// Gets the connection string for account.
        /// </summary>
        /// <param name="storageAccountName">Name of the storage account.</param>
        /// <param name="context">The storage context.</param>
        /// <returns>
        /// The connection string to <paramref name="storageAccountName" /></returns>
        string GetConnectionStringForAccount(string storageAccountName, StorageClientProviderContext context);
    }
}