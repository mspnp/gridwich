using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Service Class functional.
    /// </summary>
    public static partial class LogEventIds
    {
        // Error:

        /// <summary>Error: Invalid Uri in BlobCopyHandler</summary>
        public static readonly EventId InvalidUriInBlobCopyHandler = EventHelpers.CreateEventId(
            LogEventIdSubsystem.BlobCopy, LogEventIdLevel.Error, 0,
            "Invalid Uri in BlobCopyHandler.");
    }
}