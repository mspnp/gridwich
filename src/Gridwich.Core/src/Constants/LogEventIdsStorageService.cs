using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the StorageService.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information

        /// <summary>Information: A cached content range was found in the cache</summary>
        public static readonly EventId FoundCachedHttpRange = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Information, 0,
            "A cached content range was found in the cache.");

        /// <summary>Information: The service has finished downloading an http range</summary>
        public static readonly EventId HttpRangeDownloadedFinished = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Information, 1,
            "The service has finished downloading an http range.");

        /// <summary>Information: "Azure blob container creation progress</summary>
        public static readonly EventId ContainerCreateProgress = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Information, 2,
            "Azure blob container creation progress.");

        // Error

        /// <summary>Error: The change blob operation returned an unexpected response</summary>
        public static readonly EventId ChangeBlobTierUnexpectedResponse = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 0,
            "The change blob operation returned an unexpected response.");

        /// <summary>Error: Failed to add metadata to a blob because it does not exist.
        /// </summary>
        public static readonly EventId StorageServiceAttemptToAddMetadataToBlobThatDoesNotExist = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 1,
            "Failed to add metadata to a blob because it does not exist.");

        /// <summary>Error: Missing storage account connection string for specified URL</summary>
        public static readonly EventId StorageServiceMissingConnectionString = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 2,
            "Missing storage account connection string for specified URL.");

        /// <summary>Error: Failed to create azure blob container</summary>
        public static readonly EventId FailedToCreateContainer = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 3,
            "Failed to create azure blob container.");

        /// <summary>Error: Failed to delete azure blob container</summary>
        public static readonly EventId FailedToDeleteContainer = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 4,
            "Failed to delete azure blob container.");

        /// <summary>Error: Failed to delete azure blob container with an unexpected response from Azure</summary>
        public static readonly EventId DeleteContainerUnexpectedResponse = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 5,
            "Unexpected response while deleting container.");

        /// <summary> Error: Failed to change container access.</summary>
        public static readonly EventId FailedToChangeContainerAccess = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Error, 6,
            "Failed to change container access");

        // Critical

        /// <summary>Critical: Failed to change the tier of a blob in StorageService</summary>
        public static readonly EventId FailedToChangeTierInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 0,
            "Failed to change the tier of a blob in StorageService.");

        /// <summary>Critical: Failed to set the metadata of a blob in StorageService</summary>
        public static readonly EventId FailedToSetMetadataInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 1,
            "Failed to set the metadata of a blob in StorageService.");

        /// <summary>Critical: Failed to create a BlobBaseClient for URL</summary>
        public static readonly EventId BlobBaseClientProviderFailedToCreateBlobBaseClient = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 2,
            "Failed to create a BlobBaseClient for URL.");

        /// <summary>Critical: Unable to create a blob's SAS URL</summary>
        public static readonly EventId FailedToCreateBlobSasUriInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 3,
            "Failed to create blob SAS URL in StorageService.");

        /// <summary>Critical: Unexpected Azure storage exception when trying to delete a blob</summary>
        public static readonly EventId FailedToDeleteDueToStorageExceptionInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 4,
            "Unexpected failure deleting blob in StorageService.");

        /// <summary>Critical: Failed to create a BlobContainerClient for URL</summary>
        public static readonly EventId BlobContainerClientProviderFailedToCreateBlobContainerClient = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 5,
            "Failed to create a BlobContainerClient for URL.");

        /// <summary>Critical: Failed to create a Blob Uri in StorageService.CreateBlobUri due to missing Container name</summary>
        /// <remarks>CreateBlobUrl was asked to create a blob URL, but the parts are malformed -- likely misconfiguration.</remarks>
        public static readonly EventId StorageServiceFailingUriMissingContainer = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 6,
            "Failed to create a Blob Uri in StorageService.CreateBlobUri due to missing Container name");

        /// <summary>Critical: Failure creating the target container in BlobCopy</summary>
        public static readonly EventId StorageServiceFailedCreatingTargetContainerForBlobCopy = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 7,
            "Failed to create target container in BlobCopy");

        /// <summary>Critical: Unexpected Azure storage exception when trying to check if a blob exists</summary>
        public static readonly EventId FailedToCheckBlobExistenceDueToStorageExceptionInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 8,
            "Unexpected failure checking if blob exists in StorageService.");

        /// <summary>Critical: Unexpected Azure storage exception when trying to list the blobs of a container</summary>
        public static readonly EventId FailedToListBlobsDueToStorageExceptionInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 9,
            "Unexpected failure listing blobs in StorageService.");

        /// <summary>Critical: Failed to create a StorageBlobClientSleeve in BlobBaseClientProvider due to missing Account name</summary>
        /// <remarks>CreateBlobUrl was asked to create a blob URL, but the parts are malformed -- likely misconfiguration</remarks>
        public static readonly EventId BlobBaseClientProviderUriMissingAccountName = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 10,
            "Failed to create a StorageBlobClientSleeve in BlobBaseClientProvider due to missing Account name");

        /// <summary>Critical: Failed to create a Uri in BlobContainerClientProvider due to missing Account name</summary>
        /// <remarks>CreateBlobUrl was asked to create a blob URL, but the parts are malformed -- likely misconfiguration</remarks>
        public static readonly EventId BlobContainerClientProviderUriMissingAccountName = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 11,
            "Failed to create a Uri in BlobContainerClientProvider due to missing Account name");

        /// <summary>Critical: Failed to get blob properties in StorageService</summary>
        public static readonly EventId FailedToGetBlobPropertiesInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 12,
            "Failed to get blob properties in StorageService.");

        /// <summary>Critical: Failed to download a HTTP range for a blob in StorageService</summary>
        public static readonly EventId FailedToDownloadHttpRangeInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 13,
            "Failed to download a HTTP range for a blob in StorageService.");

        /// <summary>Critical: Failed to copy a blob in StorageService</summary>
        public static readonly EventId FailedToCopyBlobInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 14,
            "Failed to copy a blob in StorageService.");

        /// <summary>Critical: Failed to download content for a blob in StorageService</summary>
        public static readonly EventId FailedToDownloadContentInStorageService = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 15,
            "Failed to download a HTTP range for a blob in StorageService.");

        /// <summary>Critical: Failed to get account key from account name</summary>
        public static readonly EventId FailedToGetAccountKey = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageService, LogEventIdLevel.Critical, 16,
            "Failed to get account key from account name.");
    }
}
