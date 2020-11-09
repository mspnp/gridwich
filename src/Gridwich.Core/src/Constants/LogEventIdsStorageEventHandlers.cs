using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the StorageEventHandler functional area.
    /// </summary>
    public static partial class LogEventIds
    {
        // Errors:

        /// <summary>Error: Failed to get blob metadata in BlobCreatedHandler</summary>
        public static readonly EventId FailedToGetBlobMetadataInBlobCreatedHandler = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageEventHandlers, LogEventIdLevel.Error, 0,
            "Failed to get blob metadata in BlobCreatedHandler.");

        /// <summary>Error: Failed to create GridwichBlobDeletedSuccessDTO with event data in BlobDeletedHandler</summary>
        public static readonly EventId FailedToCreateBlobDeletedDataWithEventDataInBlobDeletedHandler = EventHelpers.CreateEventId(
            LogEventIdSubsystem.StorageEventHandlers, LogEventIdLevel.Error, 1,
            "Failed to create GridwichBlobDeletedSuccessDTO with event data in BlobDeletedHandler.");
    }
}
