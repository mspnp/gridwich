using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Blob Delete Operationl.
    /// </summary>
    public static partial class LogEventIds
    {
        // Warnings:

        /// <summary>Warning: Blob to delete doesn't exist</summary>
        public static readonly EventId NoSuchBlobInBlobDeleteHandler = EventHelpers.CreateEventId(
            LogEventIdSubsystem.BlobDelete, LogEventIdLevel.Warning, 0,
            "No such blob exists for delete by BlobDeleteHandler.");

        // Errors:

        /// <summary>Error: The URI for the blob to delete was malformed</summary>
        public static readonly EventId InvalidUriInBlobDeleteHandler = EventHelpers.CreateEventId(
            LogEventIdSubsystem.BlobDelete, LogEventIdLevel.Error, 0,
            "Invalid Uri in BlobDeleteHandler.");
    }
}