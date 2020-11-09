using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the MetaData storage handler.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information:

        /// <summary>Information: Success for adding metadata to blob in Create Metadata Event Handler</summary>
        public static readonly EventId SuccessEventHandlingInCreateMetadataHandler = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Metadata, LogEventIdLevel.Information, 0,
            "Success for adding metadata to blob in Create Metadata Event Handler.");
    }
}