using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Flip.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information

        /// <summary>Information: This encoder does not handle this encode type</summary>
        public static readonly EventId FlipDoesNotHandleThisType = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Flip, LogEventIdLevel.Information, 0,
            "This encoder does not handle this encode type.");

        // Critical

        /// <summary>Critical: Error SAS Uri assets</summary>
        public static readonly EventId FlipSASError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Flip, LogEventIdLevel.Critical, 0,
            "Error SAS Uri assets.");

        /// <summary>Critical: Factory does not exist</summary>
        public static readonly EventId FlipFactoryNotFound = EventHelpers.CreateEventId(
            LogEventIdSubsystem.Flip, LogEventIdLevel.Critical, 1,
            "Specified Flip Factory not found.");
    }
}