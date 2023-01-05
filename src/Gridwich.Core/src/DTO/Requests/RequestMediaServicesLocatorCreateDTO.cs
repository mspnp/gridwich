using System;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Requests the creation of a streaming locator.
    /// </summary>
    public sealed class RequestMediaServicesLocatorCreateDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the containerUri to publish.
        /// </summary>
        [JsonProperty("containerUri", Required = Required.Always)]
        public Uri ContainerUri { get; set; }

        /// <summary>
        /// Gets or sets the StreamingPolicy to apply.
        /// Expects PredefinedStreamingPolicy or custom name.
        /// </summary>
        [JsonProperty("streamingPolicyName" /*, Required = Required.Always */)]
        public string StreamingPolicyName { get; set; }

        /// <summary>
        /// Gets or sets the ContentKeyPolicy to apply.
        /// Expects a custom name.
        /// </summary>
        [JsonProperty("contentKeyPolicyName")]
        public string ContentKeyPolicyName { get; set; }

        /// <summary>
        /// Gets or sets the time based filter to apply.
        /// </summary>
        [JsonProperty("timeBasedFilter")]
        public TimeBasedFilterDTO TimeBasedFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audio filters should be generated on the asset.
        /// </summary>
        [JsonProperty("generateAudioFilters")]
        public bool GenerateAudioFilters { get; set; }
    }
}