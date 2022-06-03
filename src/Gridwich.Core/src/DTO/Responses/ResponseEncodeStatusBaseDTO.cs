using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// The data object that is serialized and sent back to Requestor when an encode is finished.
    /// </summary>
    /// <seealso cref="Gridwich.Core.DTO.ResponseBaseDTO" />
    [JsonObject(ItemRequired = Required.Always)]
    public abstract class ResponseEncodeStatusBaseDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the name of the workflow job.
        /// </summary>
        /// <value>
        /// The name of the workflow job.
        /// </value>
        public string WorkflowJobName { get; set; }

        /// <summary>
        /// Gets or sets the encoder context.
        /// </summary>
        /// <value>
        /// The encoder context.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject EncoderContext { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseEncodeStatusBaseDTO"/> class.
        /// </summary>
        /// <param name="returnEventType">The event type.</param>
        protected ResponseEncodeStatusBaseDTO(string returnEventType)
            : base(returnEventType)
        {
        }
    }

    /// <summary>
    /// Processing DTO
    /// </summary>
    /// <seealso cref="Gridwich.Core.DTO.ResponseEncodeStatusBaseDTO" />
    [JsonObject(ItemRequired = Required.Always)]
    public class ResponseEncodeProcessingDTO : ResponseEncodeStatusBaseDTO
    {
        /// <summary>
        /// Gets or sets the current status.
        /// </summary>
        /// <value>
        /// The current status.
        /// </value>
        public string CurrentStatus { get; set; }

        /// <summary>
        /// Gets or sets the percent complete.
        /// </summary>
        /// <value>
        /// The percent complete.
        /// </value>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseEncodeProcessingDTO"/> class.
        /// </summary>
        /// <param name="returnEventType">The event type.</param>
        public ResponseEncodeProcessingDTO(string returnEventType)
            : base(returnEventType)
        {
        }
    }

    /// <summary>
    /// Scheduled DTO
    /// </summary>
    /// <seealso cref="Gridwich.Core.DTO.ResponseEncodeStatusBaseDTO" />
    [JsonObject(ItemRequired = Required.Always)]
    public class ResponseEncodeScheduledDTO : ResponseEncodeStatusBaseDTO
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseEncodeScheduledDTO"/> class.
        /// </summary>
        /// <param name="returnEventType">The event type.</param>
        public ResponseEncodeScheduledDTO(string returnEventType)
            : base(returnEventType)
        {
        }
    }

    /// <summary>
    /// Success DO
    /// </summary>
    /// <seealso cref="Gridwich.Core.DTO.ResponseEncodeStatusBaseDTO" />
    [JsonObject(ItemRequired = Required.Always)]
    public class ResponseEncodeSuccessDTO : ResponseEncodeStatusBaseDTO
    {
        /// <summary>
        /// Gets or sets the outputs.
        /// </summary>
        /// <value>
        /// The outputs.
        /// </value>
        [property: SuppressMessage("Microsoft.Design", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Not applicable to DTOs")]
        public Output[] Outputs { get; set; }

        /// <summary>
        /// Gets or sets the output container.
        /// </summary>
        /// <value>
        /// The output container.
        /// </value>
        public string OutputContainer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseEncodeSuccessDTO"/> class.
        /// </summary>
        /// <param name="returnEventType">The event type to publish on Event Grid.</param>
        public ResponseEncodeSuccessDTO(string returnEventType)
            : base(returnEventType)
        {
        }
    }

    /// <summary>
    /// Dispatch DTO
    /// </summary>
    /// <seealso cref="Gridwich.Core.DTO.ResponseEncodeStatusBaseDTO" />
    [JsonObject(ItemRequired = Required.Always)]
    public class ResponseEncodeDispatchedDTO : ResponseEncodeStatusBaseDTO
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseEncodeDispatchedDTO"/> class.
        /// </summary>
        /// <param name="returnEventType">The event type to return to Event Grid.</param>
        public ResponseEncodeDispatchedDTO(string returnEventType)
            : base(returnEventType)
        {
        }
    }

    // [Deprecated]

    /// <summary>
    /// Failure DTO
    /// </summary>
    /// <seealso cref="Gridwich.Core.DTO.ResponseEncodeStatusBaseDTO" />
    public class ResponseEncodeFailureDTO : ResponseEncodeStatusBaseDTO
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseEncodeFailureDTO"/> class.
        /// </summary>
        public ResponseEncodeFailureDTO()
            : base(CustomEventTypes.ResponseFailure)
        {
        }
    }

    /// <summary>
    /// Output object
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class Output
    {
        /// <summary>
        /// Gets or sets the BLOB URI.
        /// </summary>
        /// <value>
        /// The BLOB URI.
        /// </value>
        public object BlobUri { get; set; }
    }
}
