using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;

using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Bases
{
    /// <summary>
    /// Base class for event grid handlers.
    /// </summary>
    /// <typeparam name="TConcreteClass">The implementation of the EventGridHandler.</typeparam>
    /// <typeparam name="TEventData">The input type for the data that is going to be handled by this handler.</typeparam>
    public abstract class EventGridHandlerBase<TConcreteClass, TEventData> : IEventGridHandler
    {
        /// <summary>
        /// Gets the object logger to be used by the base and concrete class.
        /// </summary>
        protected IObjectLogger<TConcreteClass> Log { get; }

        /// <summary>
        /// String to represent any version of the event.
        /// </summary>
        protected const string AnyVersion = "*";

        /// <summary>
        /// String array to represent any version of the event.
        /// </summary>
        protected static readonly string[] AllVersionList = new string[] { AnyVersion };
        private readonly IEventGridPublisher _eventPublisher;
        private readonly string _handlerId;
        private readonly IDictionary<string, string[]> _acceptedEvents;


        /// <summary>
        /// The base implementation of the IEventGridHandler.HandlesEvent.
        /// </summary>
        /// <param name="eventType">The event type which is being queried.</param>
        /// <param name="dataVersion">The dataVersion which is being queries.</param>
        /// <returns>True if this handler has opted to handle this type and version of events.</returns>
        public virtual bool HandlesEvent(string eventType, string dataVersion)
        {
            if (!_acceptedEvents.ContainsKey(eventType))
            {
                return false;
            }

            foreach (var acceptedVersion in _acceptedEvents[eventType])
            {
                if (acceptedVersion == AnyVersion)
                {
                    return true;
                }
                if (acceptedVersion == dataVersion)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridHandlerBase{TConcreteClass, TEventData}"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="handlerId">handlerId.</param>
        /// <param name="acceptedEvents">acceptedEvents.</param>
        /// <param name="topicNameToPublishTo">topicNameToPublishTo.</param>
        protected EventGridHandlerBase(
            IObjectLogger<TConcreteClass> logger,
            IEventGridPublisher eventPublisher,
            string handlerId,
            IDictionary<string, string[]> acceptedEvents)
        {
            _eventPublisher = eventPublisher;
            _handlerId = handlerId;
            _acceptedEvents = new Dictionary<string, string[]>(acceptedEvents);
            Log = logger;
        }

        /// <inheritdoc/>
        public string GetHandlerId()
        {
            return _handlerId;
        }

        /// <inheritdoc/>
        public async Task<bool> HandleAsync(EventGridEvent eventGridEvent)
        {
            _ = eventGridEvent ?? throw new ArgumentNullException(nameof(eventGridEvent));
            _ = eventGridEvent.Data ?? throw new ArgumentException("Input event data cannot be null");

            if (!HandlesEvent(eventGridEvent.EventType, eventGridEvent.DataVersion))
            {
                Log.LogEventObject(LogEventIds.EventNotSupported, eventGridEvent);
                var msg = $"This handler does not support EventType {eventGridEvent.EventType} with DataVersion {eventGridEvent.DataVersion}. Make sure to check with {nameof(HandlesEvent)} before calling {nameof(HandleAsync)}.";
                throw new ArgumentException(msg);
            }

            TEventData eventData;
            JObject operationContext;

            try
            {
                eventData = GetEventData(eventGridEvent);
                operationContext = GetOperationContext(eventData);
            }
            catch (Exception e)
            {
                await PublishGridwichException(e, StorageClientProviderContext.None.ClientRequestIdAsJObject, eventGridEvent).ConfigureAwait(false);
                return false;
            }

            try
            {
                var result = await HandleAsyncInternal(eventGridEvent, eventData, operationContext).ConfigureAwait(false);
                return result;
            }
            catch (Exception e)
            {
                await PublishGridwichException(e, operationContext, eventGridEvent).ConfigureAwait(false);
                return false;
            }
        }

        private async Task<bool> HandleAsyncInternal(EventGridEvent eventGridEvent, TEventData eventData, JObject operationContext)
        {
            if (eventGridEvent.EventType.StartsWith(CustomEventTypes.RequestCommonPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.LogEventObject(LogEventIds.StartingAcknowledgement, eventGridEvent);
                await AcknowledgeRequestEvent(eventGridEvent, operationContext).ConfigureAwait(false); // What happens if ack fails to publish?
                Log.LogEventObject(LogEventIds.FinishedAcknowledgement, eventGridEvent);
            }

            if (eventData == null)
            {
                throw new GridwichArgumentException(nameof(eventData), "Event data deserialized to null.",
                    LogEventIds.FailedToDeserializeEventData, operationContext);
            }

            Log.LogEventObject(LogEventIds.StartingEventHandling, eventGridEvent);

            var responseDto = await DoWorkAsync(eventData, eventGridEvent.EventType).ConfigureAwait(false);

            var eventWasHandled = false;

            if (responseDto.DoNotPublish)
            {
                // If we were told not to publish, we can classify getting to this point as success,
                // regardless of whether the actual event represents a failure or success.
                eventWasHandled = true;
            }
            else
            {
                // Yes, we should publish the event.
                // Final status depends on success Publishing the event
                // TODO: Do we care about the subject of an event?
                var gridEvent = GetEventGridEventFromResponseDTO(responseDto, $"/{responseDto.ReturnEventType}");
                eventWasHandled = await PublishEventToTopicAsync(gridEvent, eventGridEvent).ConfigureAwait(false);
            }

            Log.LogEventObject(LogEventIds.FinishedEventHandling, eventGridEvent);
            return eventWasHandled;
        }

        /// <summary>
        /// Get the event data from the event grid object by deserializing to this handlers input type.
        /// Override if a custom operation is required to extract the data.
        /// </summary>
        /// <param name="eventGridEvent">The input event from which the data will be extracted and parsed. Data is expected to be JSON.</param>
        /// <returns>An instance of the input TEventData specified.</returns>
        protected virtual TEventData GetEventData(EventGridEvent eventGridEvent)
        {
            _ = eventGridEvent ?? throw new ArgumentNullException(nameof(eventGridEvent));

            // Do we want to catch parsing exceptions and re throw them wrapped?
            var result = JsonConvert.DeserializeObject<TEventData>(eventGridEvent.Data.ToString());
            return result;
        }

        private JObject GetOperationContext(TEventData eventData)
        {
            var result = eventData is RequestBaseDTO dto ? dto.OperationContext : GetOperationContextForExternalEvent(eventData);
            return result;
        }

        private JObject GetOperationContextForExternalEvent(TEventData eventData)
        {
            var result = eventData is IExternalEventData data ? data.GetOperationContext() : ParseOperationContext(eventData);
            return result;
        }

        /// <summary>
        /// Parse the operation context from the event data when direct access to the model is required. Override if a
        /// custom operation is required for parsing the operation context.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <returns>The operation context.</returns>
        protected virtual JObject ParseOperationContext(TEventData eventData)
        {
            return null;
        }

        private async Task AcknowledgeRequestEvent(EventGridEvent eventGridEvent, JObject operationContext)
        {
            var responseAck = new ResponseAcknowledgeDTO(eventGridEvent.EventType)
            {
                OperationContext = operationContext
            };
            var ackEvent = GetEventGridEventFromResponseDTO(responseAck, eventGridEvent.Subject);

            var isAck = await PublishEventToTopicAsync(ackEvent, eventGridEvent).ConfigureAwait(false);

            if (!isAck)
            {
                Log.LogEventObject(LogEventIds.FailedToAcknowledge, eventGridEvent);
            }
        }

        private async Task PublishGridwichException(Exception ex, JObject operationContext, EventGridEvent inputEvent)
        {
            GridwichException gridwichException = ex is GridwichException exception ? exception : new GridwichUnhandledException("Unhandled exception in EventGridHandlerBase.", ex, operationContext);
            gridwichException.AddOperationContext(operationContext);
            Log.LogExceptionObject(out Uri locator, gridwichException.LogEventId, gridwichException, inputEvent);
            EventGridEvent failureEventGridEvent = gridwichException.ToGridwichFailureEventGridEvent(GetHandlerId(), GetType(), locator);

            await PublishEventToTopicAsync(failureEventGridEvent, inputEvent).ConfigureAwait(false);
        }

        private async Task<bool> PublishEventToTopicAsync(EventGridEvent outputEvent, EventGridEvent inputEvent)
        {
            bool eventPublished;
            try
            {
                eventPublished = await _eventPublisher.PublishEventToTopic(outputEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.LogExceptionObject(LogEventIds.FailedCriticallyToPublishEvent, ex, new { inputEvent, outputEvent });
                return false;
            }

            if (!eventPublished)
            {
                Log.LogEventObject(LogEventIds.FailedToPublishEvent, new { inputEvent, outputEvent });
            }

            return eventPublished;
        }

        /// <summary>
        /// Process the event grid event and return a response event.
        /// </summary>
        /// <param name="eventData">The event grid event.</param>
        /// <param name="eventType">The event type.</param>
        /// <returns>An <see cref="EventGridEvent"/></returns>
        protected abstract Task<ResponseBaseDTO> DoWorkAsync(TEventData eventData, string eventType);

        /// <summary>
        /// Get an event grid event based on the input parameters. This method accepts any <see cref="ResponseBaseDTO" />.
        /// Event type will be extracted from the DTO.
        /// </summary>
        /// <param name="anyDto">Any dto.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="appendEventIdToSubject">if set to <c>true</c> [append event identifier to subject].</param>
        /// <param name="dataVersion">The data version.</param>
        /// <param name="locator">The locator.</param>
        /// <returns>
        /// An <see cref="EventGridEvent" />
        /// </returns>
        public EventGridEvent GetEventGridEventFromResponseDTO(ResponseBaseDTO anyDto, string subject, bool appendEventIdToSubject = true, string dataVersion = "1.0", EventLocator locator = null)
        {
            if (anyDto == null)
            {
                throw new ArgumentNullException(nameof(anyDto));
            }

            var eventGridEventId = Guid.NewGuid().ToString();

            var eventToPublish = new EventGridEvent
            {
                Id = eventGridEventId,
                Data = anyDto,
                EventTime = DateTime.UtcNow,
                EventType = anyDto.ReturnEventType,
                Subject = appendEventIdToSubject ? $"{subject}/{eventGridEventId}" : subject,
                DataVersion = dataVersion,
            };

            return locator == null ? eventToPublish : eventToPublish.DecorateWith(locator);
        }

        /// <summary>
        /// Get a <see cref="ResponseFailureDTO" /> based on the input parameters.
        /// </summary>
        /// <param name="failureMessage">The failure message.</param>
        /// <param name="operationContext">The operation context.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="uriLocator">The Uri locator.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>
        /// An <see cref="EventGridEvent" />
        /// </returns>
        protected ResponseFailureDTO GetGridwichFailureDTO(string failureMessage, JObject operationContext, EventId eventId, Uri uriLocator, Exception exception = null)
        {
            _ = uriLocator ?? throw new ArgumentNullException(nameof(uriLocator));

            var dtoFailureMessage = exception == null ? failureMessage : exception.Message;

            return new ResponseFailureDTO()
            {
                LogEventMessage = dtoFailureMessage,
                LogRecordId = Guid.NewGuid().ToString(),
                LogRecordUrl = new Uri(uriLocator.AbsoluteUri),
                LogEventId = eventId.Id,
                EventHandlerClassName = GetType().Name,
                HandlerId = GetHandlerId(),
                OperationContext = operationContext,
            };
        }
    }
}
