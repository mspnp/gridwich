using Gridwich.Core.Constants;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// An event acknowledgement.
    /// </summary>
    public class ResponseAcknowledgeDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the EventType for the data of this DTO.
        /// </summary>
        [JsonProperty("eventType")]
        public string EventType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseAcknowledgeDTO"/> class.
        /// </summary>
        /// <param name="eventType">Event type.</param>
        public ResponseAcknowledgeDTO(string eventType)
            : base(CustomEventTypes.ResponseAcknowledge)
        {
            this.EventType = eventType;
        }
    }
}