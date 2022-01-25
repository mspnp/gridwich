using Gridwich.Core.Helpers;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Constants
{
    /// <summary>
    /// This partial class contains all the event ids for the Service Class functional.
    /// </summary>
    public static partial class LogEventIds
    {
        // Information:

        /// <summary>Information: Received event subscription validation message</summary>
        public static readonly EventId ReceivedEventGridSubscriptionValidationEventType = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 0,
            "Received event subscription validation message.");

        /// <summary>Information: Received event message</summary>
        public static readonly EventId ReceivedEventGridEventType = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 1,
            "Received event message.");

        /// <summary>Information: About to loop through all handlers for event</summary>
        public static readonly EventId CallingAllHandlersForEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 2,
            "About to loop through all handlers for event.");

        /// <summary>Information: Called Event Handler for event</summary>
        public static readonly EventId HandlerCalledForEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 3,
            "Called Event Handler for event.");

        /// <summary>Information: All handlers called for event message</summary>
        public static readonly EventId EventsHandledByAllSubscribedHandlers = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 4,
            "All handlers called for event message.");

        /// <summary>Information: Going to publish event with event Id</summary>
        public static readonly EventId AboutToAttemptPublishOfEventWithId = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 5,
            "Going to publish event with event Id.");

        /// <summary>Information: Publish event to topic</summary>
        public static readonly EventId PublishedEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 6,
            "Publish event to topic.");

        /// <summary>Information: Received OperationCancelException</summary>
        public static readonly EventId OperationCancelException = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 7,
            "Received OperationCancelException.");

        /// <summary>Information: Received request to shutdown function app</summary>
        public static readonly EventId FunctionAppShuttingDown = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Information, 8,
            "Received request to shutdown function app.");

        /// <summary>Error: Logging object parsing failed</summary>
        public static readonly EventId ObjectLoggerParsingError = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 0,
            "Logging object parsing failed.");

        /// <summary>Error: Received event message and no handler exists for it</summary>
        public static readonly EventId ExceptionHandlingEventGridEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 1,
            "Received event message and no handler exists for it.");

        /// <summary>Error: Received event message and no handler exists for it</summary>
        public static readonly EventId UnhandledEventGridEventType = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 2,
            "Received event message and no handler exists for it.");

        /// <summary>Error: Received exception while calling handler for event</summary>
        public static readonly EventId ExceptionCallingHandlerForEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 3,
            "Received exception while calling handler for event.");

        /// <summary>Error: Received exception while publishing event</summary>
        public static readonly EventId ExceptionPublishingEvent = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 4,
            "Received exception while publishing event.");

        /// <summary>Error: Null HttpRequest object</summary>
        public static readonly EventId EventGridFunctionGotNullHttpRequest = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 5,
            "Null HttpRequest object.");

        /// <summary>Error: Exception while reading HttpRequest.Body</summary>
        public static readonly EventId EventGridFunctionExceptionReadingHttpRequestBody = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 6,
            "Exception while reading HttpRequest.Body.");

        /// <summary>Error: Null HttpRequest.Body object. Expected List&lt;ventGridEvent&gt;</summary>
        public static readonly EventId EventGridFunctionGotNullHttpRequestBody = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 7,
            "Null HttpRequest.Body object. Expected List<EventGridEvent>.");

        /// <summary>Error: Got empty request body. Expected List&lt;ventGridEvent&gt;</summary>
        public static readonly EventId EventGridFunctionGotEmptyBody = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 8,
            "Got empty request body. Expected List<EventGridEvent>.");

        /// <summary>Error: Unable to parse body. Expected List&lt;ventGridEvent&gt;</summary>
        public static readonly EventId EventGridFunctionGotUnparsableBody = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 9,
            "Unable to parse body. Expected List<EventGridEvent>.");

        /// <summary>Error: Got empty array in request body. Expected List&lt;EventGridEvent&gt;</summary>
        public static readonly EventId EventGridFunctionGotEmptyArrayAsBody = EventHelpers.CreateEventId(
            LogEventIdSubsystem.App, LogEventIdLevel.Error, 10,
            "Got empty array in request body. Expected List<EventGridEvent>");
    }
}