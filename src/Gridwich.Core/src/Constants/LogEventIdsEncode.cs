using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Encode area.
    /// </summary>
    public static partial class LogEventIds
    {
        // Error:

        /// <summary>Error: Encoder failed to function as expected</summary>
        public static readonly EventId EncodeCompleteFailure = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Encode, LogEventIdLevel.Error, 0,
            "Encode Failed.");

        /// <summary>Error: Error parsing output data from encoder</summary>
        public static readonly EventId EncodeOutputFailure = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Encode, LogEventIdLevel.Error, 1,
            "Encode output data formatting error.");

        // Critical:

        /// <summary>Critical: Error parsing EventGrid Data block in Encode Request EventGrid handler</summary>
        public static readonly EventId EncodeRequestJSONParseError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Encode, LogEventIdLevel.Critical, 0,
            "Error parsing EventGrid Data block in Encode Request EventGrid handler.");
    }
}