using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Allows the caller to specify the options used for the Media Services encoder.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class RequestMediaServicesV2EncodeCreateDTO : RequestEncodeCreateDTO
    {
        /// <summary>
        /// Gets or sets the name of the Azure Media Services v2 preset to use.
        /// </summary>
        [JsonProperty("presetName")]
        public string PresetName { get; set; }

        /// <summary>
        /// Gets or sets the time in decimal seconds for the location of the thumbnail.
        /// </summary>
        [JsonProperty("thumbnailTimeSeconds", Required = Required.Default)]
        public double ThumbnailTimeSeconds { get; set; }
    }
}