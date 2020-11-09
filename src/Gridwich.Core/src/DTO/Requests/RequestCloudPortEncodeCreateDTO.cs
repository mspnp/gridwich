using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This is the data that is specific to a Cloud Port encode.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class RequestCloudPortEncodeCreateDTO : RequestEncodeCreateDTO
    {
        /// <summary>
        /// Gets or sets the Telestream Cloud workflow name.
        /// </summary>
        [JsonProperty("workflowName")]
        public string WorkflowName { get; set; }

        /// <summary>
        /// Gets or sets any parameters that need to be passed to CloudPort.
        /// </summary>
        [JsonProperty("parameters", Required = Required.Default)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JArray Parameters { get; set; }

        /// <summary>
        /// Gets or sets the secToLive of any source SASes generated.
        /// </summary>
        [JsonProperty("secToLive", Required = Required.Default)]
        public int SecToLive { get; set; }
    }
}