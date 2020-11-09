using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Analysis Class functional area.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information:

        /// <summary>Information: About to call main function to do the analysis on the video</summary>
        public static readonly EventId AboutToCallAnalysisDeliveryEntry = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Analysis, LogEventIdLevel.Information, 0,
            "About to call main function to do the analysis on the video");

        /// <summary>Information: Got a successful results from analysis on file</summary>
        public static readonly EventId AnalysisOfDeliveryFileSuccessful = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Analysis, LogEventIdLevel.Information, 1,
            "Got a successful results from analysis on file");

        /// <summary>Information: Got a NULL results from analysis on file</summary>
        public static readonly EventId AnalysisOfDeliveryFileFailed = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Analysis, LogEventIdLevel.Information, 2,
            "Got a NULL results from analysis on file");
    }
}