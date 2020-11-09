namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the StorageService.
    /// </summary>
    public static class StorageServiceConstants
    {
        /// <summary>
        /// Special value for GetOrDownloadContentAsync length parameter.  If you pass this value
        /// for the desiredSize argument, the concrete StorageService instance will use it's default
        /// length (currently 4MB).
        /// <summary>
        public const long UseDefaultBufferSize = -1L;

        /// <summary>Gridwich-wide default for blob rehydration (change blob tier) priority</summary>
        public const BlobRehydratePriority DefaultRehydratePriority = BlobRehydratePriority.Normal;

        /// <summary>
        /// Default minutes for Time To Live (ttl).
        /// <summary>
        public const int DefaultTimeToLiveMinutes = 5;

        /// <summary>Number of seconds that we should "back-date" a SAS URL to account for possible clock skew</summary>
        /// <remarks>
        /// As this will be subtracted from the current time, there is no introduced exposure because
        /// the computed time will (subject to a bit of possible skew) be in the past.
        /// </remarks>
        public const int SecondsToBackDateForSasUrl = 300; // 5 minutes

        /// <summary>Standard Azure Storage DNS account suffix.  e.g. myaccount.core.windows.net</summary>
        public const string AzureStorageDnsSuffix = "core.windows.net";

        /// <summary>Standard Azure Storage DNS account suffix for the blob service.
        /// e.g. myaccount.blob.core.windows.net
        /// </summary>
        public const string AzureStorageDnsBlobSuffix = "blob." + AzureStorageDnsSuffix;

        /// <summary>The protocol to be used for connection strings for and requests to Azure Storage.</summary>
        public const string AzureStorageProtocol = "https";
    }
}
