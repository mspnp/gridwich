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

        /// <summary>Information: Starting to acknowledge event</summary>
        public static readonly EventId StartingAcknowledgement = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Information, 0,
            "Starting to acknowledge event");

        /// <summary>Information: Finished acknowledging event</summary>
        public static readonly EventId FinishedAcknowledgement = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Information, 1,
            "Finished acknowledging event");

        /// <summary>Information: Starting to handle event</summary>
        public static readonly EventId StartingEventHandling = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Information, 2,
            "Starting to handle event");

        /// <summary>Information: Finished handling event</summary>
        public static readonly EventId FinishedEventHandling = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Information, 3,
            "Finished handling event");

        // Errors:

        /// <summary>Error: Failed to send acknowledge event</summary>
        public static readonly EventId FailedToAcknowledge = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Error, 0,
            "Failed to send acknowledge event");

        /// <summary>Error: Event is not supported in this EventHandler</summary>
        public static readonly EventId EventNotSupported = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Error, 1,
            "Event is not supported in this EventHandler");

        /// <summary>Error: Failed to publish event to event topic</summary>
        public static readonly EventId FailedToPublishEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Error, 2,
            "Failed to publish event to event topic");

        /// <summary>Error: Failed to deserialize the input event grid event</summary>
        public static readonly EventId FailedToDeserializeEventData = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Error, 3,
            "Failed to deserialize the input event grid event.");

        /// <summary>Error: Unhandled exception in code</summary>
        public static readonly EventId GridwichUnhandledException = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Error, 4,
            "Unhandled/Unknown exception.");

        // Critical

        /// <summary>Critical: Found an exception while trying to publish event to event topic.</summary>
        public static readonly EventId FailedCriticallyToPublishEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.EventHandlerCore, LogEventIdLevel.Critical, 0,
            "Found an exception while trying to publish event to event topic.");
    }
}
