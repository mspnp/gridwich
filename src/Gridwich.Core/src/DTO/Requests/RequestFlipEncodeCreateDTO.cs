using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This is the data that is specific to a Flip encode.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class RequestFlipEncodeCreateDTO : RequestEncodeCreateDTO
    {
        /// <summary>
        /// Gets or sets the name of the Telestream Cloud Flip Factory.
        /// </summary>
        [JsonProperty("factoryName")]
        public string FactoryName { get; set; }


        /// <summary>
        /// Gets or sets the comma separated list of profiles to encode on the profile.
        /// </summary>
        [JsonProperty("profiles")]
        public string Profiles { get; set; }

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