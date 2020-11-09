using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Models
{
    /// <summary>
    /// This object represents the JSON Payload string passed by the Encode Service to the Flip Encode CreateVideo request.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class FlipPayload
    {
        /// <summary>
        /// Gets or sets a GUID created by Requestor and passed thru the system to identify the workflow instance of the EncodeRequest.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject OperationContext { get; set; }

        /// <summary>
        /// Gets or sets the factory identifier.
        /// </summary>
        public string FactoryId { get; set; }

        /// <summary>
        /// Gets or sets tunnels the blob outputContainer thru the Flip payload.
        /// </summary>
        public string OutputContainer { get; set; }
    }
}