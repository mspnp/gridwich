using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Allows the caller to specify the options used for the Media Services encoder.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class RequestMediaServicesV3EncodeCreateDTO : RequestEncodeCreateDTO
    {
        /// <summary>
        /// Gets or sets the name of the Azure Media Services v3 transform to use.
        /// </summary>
        [JsonProperty("transformName")]
        public string TransformName { get; set; }

        /// <summary>
        /// Gets or sets the time based filter to apply.
        /// </summary>
        [JsonProperty("timeBasedEncode", Required = Required.Default)]
        public TimeBasedEncodeDTO TimeBasedEncode { get; set; }
    }
}