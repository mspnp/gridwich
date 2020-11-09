using System;
using System.Collections.Generic;

using Gridwich.Core.Constants;
using Gridwich.Core.DTO;

using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Exceptions
{
    /// <summary>
    /// Gridwich Base Exception.
    ///
    /// Derive your service-specific exceptions from this class.
    /// Your derived type should, at a minimum, have the same constructor arguments as this class.
    /// You are encouraged to extend the constructor arguments to impose additional detailed
    /// information, which you can save via Data.Add("myParam","theValue").
    /// </summary>
    public abstract class GridwichException : Exception
    {
        /// <summary>
        /// Gets the last LogEventId that was logged relevant to this exception.
        /// </summary>
        public EventId LogEventId { get; }

        /// <summary>
        /// Gets the OperationContext for this exception.
        /// </summary>
        public JObject OperationContext { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichException"/> class.
        /// Use this exception directly, or as base for specific service exceptions.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        protected GridwichException(string message, EventId logEventId, JObject operationContext)
            : base(message)
        {
            LogEventId = logEventId;
            OperationContext = operationContext;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichException"/> class.
        /// Use this exception directly, or as base for specific service exceptions.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        protected GridwichException(string message, EventId logEventId, JObject operationContext, Exception innerException)
            : base(message, innerException)
        {
            LogEventId = logEventId;
            OperationContext = operationContext;
        }

        /// <summary>
        /// Base implementation to convert into a <see cref="GridwichFailureDTO">.
        /// </summary>
        /// <param name="handlerId">Caller's handlerId.</param>
        /// <param name="eventHandlerClass">Caller's eventHandlerClass.</param>
        /// <param name="uriLocator">Caller's uriLocator.</param>
        /// <returns>GridwichFailureDTO</returns>
        public virtual ResponseFailureDTO ToGridwichFailureDTO(string handlerId, Type eventHandlerClass, Uri uriLocator = null)
        {
            if (string.IsNullOrEmpty(handlerId))
            {
                throw new ArgumentException($@"{nameof(handlerId)} is invalid", nameof(handlerId));
            }

            _ = eventHandlerClass ?? throw new ArgumentNullException(nameof(eventHandlerClass));

            var responseFailureDTO = new ResponseFailureDTO
            {
                LogEventMessage = Message,
                LogRecordUrl = (uriLocator == null) ? null : new Uri(uriLocator.AbsoluteUri),
                LogEventId = LogEventId.Id,
                EventHandlerClassName = eventHandlerClass.Name,
                HandlerId = handlerId,
                OperationContext = OperationContext,
                Details = GetExceptionChainDetails(this)
            };

            return responseFailureDTO;
        }

        /// <summary>
        /// Base implementation to convert into a GridwichFailureDTO in an EventGridEvent.
        /// </summary>
        /// <param name="handlerId">Caller's handlerId.</param>
        /// <param name="eventHandlerClass">Caller's eventHandlerClass.</param>
        /// <param name="uriLocator">Caller's uriLocator.</param>
        /// <returns>EventGridEvent.</returns>
        public virtual EventGridEvent ToGridwichFailureEventGridEvent(string handlerId, Type eventHandlerClass, Uri uriLocator = null)
        {
            var responseFailureDTO = ToGridwichFailureDTO(handlerId, eventHandlerClass, uriLocator);

            var eventToPublish = new EventGridEvent()
            {
                Id = Guid.NewGuid().ToString(),
                Data = responseFailureDTO,
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.ResponseFailure,
                Subject = $"gridwichFailure/{handlerId}/{responseFailureDTO.LogEventId}",
                DataVersion = "1.0",
            };

            return eventToPublish;
        }

        /// <summary>
        /// Safely add data to the <see cref="Exception.Data"/>, checking if both the key is not null, empty or whitespace
        /// and if the value is not null.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>True if added to <see cref="Exception.Data"/>. False otherwise.</returns>
        public bool SafeAddToData(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key) && value != null && !Data.Contains(key))
            {
                Data.Add(key, value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// If the exception does not have an operation context, add the supplied one.
        /// </summary>
        /// <param name="operationContext">An operation context to add.</param>
        public void AddOperationContext(JObject operationContext)
        {
            if (operationContext is null || !(this.OperationContext is null))
            {
                return;
            }
            else
            {
                OperationContext = operationContext;
            }
        }

        private IEnumerable<ExceptionChainDetailDTO> GetExceptionChainDetails(Exception e)
        {
            var chain = new List<ExceptionChainDetailDTO>();
            var currentInner = e.InnerException;

            while (currentInner != null)
            {
                chain.Add(new ExceptionChainDetailDTO()
                {
                    Data = currentInner.Data,
                    Message = currentInner.Message,
                });

                if (currentInner is AggregateException)
                {
                    var aggException = currentInner as AggregateException;

                    foreach (var aggInnerException in aggException.InnerExceptions)
                    {
                        var exceptionChain = GetExceptionChainDetails(aggInnerException);

                        chain.AddRange(exceptionChain);
                    }
                }

                currentInner = currentInner.InnerException;
            }

            return chain;
        }
    }
}
