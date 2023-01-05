using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class is a base class for all Gridwich DTOs.
    /// </summary>
    public abstract class ResponseBaseDTO
    {
        /// <summary>
        /// Gets the EventType of the resulting event grid event. This will NOT get serialized
        /// as part of the data of any event as it is ignored by the JSON converter.
        /// </summary>
        [JsonIgnore]
        public string ReturnEventType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBaseDTO"/> class.
        /// </summary>
        /// <param name="returnEventType">The event type related to this DTO.</param>
        protected ResponseBaseDTO(string returnEventType)
        {
            ReturnEventType = returnEventType;
        }

        /// <summary>
        /// Gets or sets the OperationContext.
        /// </summary>
        [JsonProperty("operationContext")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject OperationContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if true, do not published the event to EventGrid.  This is to allow
        /// handlers like BlobCreated/BlobDeleted to return a meaningful result
        /// type from DoWorkAsync(), but control whether  the EventGridHandlerBase
        /// republishes the event to EventGrid.
        /// </summary>
        /// <remarks>
        /// This flag will currently ever be set by BlobCreated/BlobDeleted, which
        /// process Azure Storage notifications.  As dictated by their intrepretation
        /// of the StorageContext as Muted/Not-Muted, they need control over whether
        /// Requestor will be informed of the notification.
        ///
        /// Notes:<ul>
        /// <li>This is never serialized as it is control-related, rather than strictly part of the event.
        /// <li>The awkward double-negative naming (DoNotPublish == false => DoPublish) is intentional to
        /// ensure that the default initialization of false covers the 99% case which is "yes, do publish".
        /// </ul>
        [JsonIgnore]
        public bool DoNotPublish { get; set; }
    }
}