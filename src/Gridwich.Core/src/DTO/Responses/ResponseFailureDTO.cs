using Gridwich.Core.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// The results of an Analysis.
    /// </summary>
    public class ResponseFailureDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the LogEventId.
        /// </summary>
        [JsonProperty("logEventId")]
        public int LogEventId { get; set; }

        /// <summary>
        /// Gets or sets the LogEventMessage.
        /// </summary>
        [JsonProperty("logEventMessage")]
        public string LogEventMessage { get; set; }

        /// <summary>
        /// Gets or sets the logRecordId guid as a string.
        /// </summary>
        [JsonProperty("logRecordId")]
        public string LogRecordId { get; set; }

        /// <summary>
        /// Gets or sets the logRecordUrl.
        /// </summary>
        [JsonProperty("logRecordUrl")]
        public Uri LogRecordUrl { get; set; }

        /// <summary>
        /// Gets or sets the EventHandlerClassName.
        /// </summary>
        [JsonProperty("eventHandlerClassName")]
        public string EventHandlerClassName { get; set; }

        /// <summary>
        /// Gets or sets the HandlerId guid as a string.
        /// </summary>
        [JsonProperty("handlerId")]
        public string HandlerId { get; set; }

        /// <summary>
        /// Gets or sets the ExceptionChainDetailDTO.
        /// </summary>
        [JsonProperty("details")]
        public IEnumerable<ExceptionChainDetailDTO> Details { get; set; }

        /// Initializes a new instance of the <see cref="ResponseFailureDTO"/> class.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        public ResponseFailureDTO()
            : base(CustomEventTypes.ResponseFailure)
        {
        }
    }
}