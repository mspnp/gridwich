using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

using Newtonsoft.Json;

namespace Gridwich.Core.EventGrid
{
    /// <summary>
    /// Dispatches event grid events to matching event grid handlers.
    /// </summary>
    public class EventGridDispatcher : IEventGridDispatcher
    {
        private readonly IObjectLogger<EventGridDispatcher> _logger;
        private readonly IEnumerable<IEventGridHandler> _eventGridHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridDispatcher"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="eventGridHandlers">eventGridHandlers.</param>
        public EventGridDispatcher(
            IObjectLogger<EventGridDispatcher> logger,
            IEnumerable<IEventGridHandler> eventGridHandlers)
        {
            _logger = logger;
            _eventGridHandlers = eventGridHandlers;
        }

        /// <inheritdoc/>
        public async Task<object> DispatchEventGridEvents(List<EventGridEvent> eventGridEvents)
        {
            _ = eventGridEvents ?? throw new ArgumentNullException(nameof(eventGridEvents));

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                // Handle Subscription Validation Request:
                if (eventGridEvent.EventType == EventTypes.EventGridSubscriptionValidationEvent)
                {
                    _logger.LogEventObject(LogEventIds.ReceivedEventGridSubscriptionValidationEventType, new { EventTypes = eventGridEvent.EventType });
                    var eventData = JsonConvert.DeserializeObject<SubscriptionValidationEventData>(eventGridEvent.Data.ToString());

                    var responseData = new SubscriptionValidationResponse()
                    {
                        ValidationResponse = eventData.ValidationCode
                    };
                    return new OkObjectResult(responseData);
                }

                try
                {
                    // Handle other event types
                    _ = await HandleEventGridEvent(eventGridEvent).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    var eventInfo = new { eventGridEvent.Id, eventGridEvent.EventType, eventGridEvent.DataVersion };
                    _logger.LogExceptionObject(LogEventIds.ExceptionHandlingEventGridEvent, e, eventInfo);
                }
            }

            return new OkResult();
        }

        private async Task<bool> HandleEventGridEvent(EventGridEvent eventGridEvent)
        {
            var eventInfo = new { eventGridEvent.Id, eventGridEvent.EventType, eventGridEvent.DataVersion };
            _logger.LogEventObject(LogEventIds.ReceivedEventGridEventType, eventInfo);
            var validEventHandlers = _eventGridHandlers
                .Where(h => h.HandlesEvent(eventGridEvent.EventType, eventGridEvent.DataVersion))
                .ToList();

            if (validEventHandlers.Count == 0)
            {
                _logger.LogEventObject(LogEventIds.UnhandledEventGridEventType, eventInfo);
                return false;
            }

            _logger.LogEventObject(LogEventIds.CallingAllHandlersForEvent, eventGridEvent);

            var results = await Task.WhenAll(validEventHandlers.Select(async (handler) =>
            {
                bool handled = false;
                try
                {
                    handled = await handler.HandleAsync(eventGridEvent).ConfigureAwait(false);
                    _logger.LogEventObject(LogEventIds.HandlerCalledForEvent, new { eventInfo, HandlerId = handler.GetHandlerId(), handled });
                }
                catch (Exception e)
                {
                    _logger.LogExceptionObject(LogEventIds.ExceptionCallingHandlerForEvent, e, eventInfo);
                }

                return handled;
            })).ConfigureAwait(false);

            bool handledByAllHandlers = results.ToList().All(r => r);

            _logger.LogEventObject(LogEventIds.EventsHandledByAllSubscribedHandlers, new { eventGridEvent.Id, handledByAllHandlers });
            return handledByAllHandlers;
        }
    }
}