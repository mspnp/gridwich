using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Allows the specification of a subset of time within a media asset.
    /// </summary>
    public sealed class TimeBasedFilterDTO
    {
        /// <summary>
        /// Gets or sets the decimal seconds to start trim an encode.
        /// </summary>
        [JsonProperty("startSeconds")]
        public double StartSeconds { get; set; }

        /// <summary>
        /// Gets or sets the decimal seconds to end trim an encode.
        /// </summary>
        [JsonProperty("endSeconds")]
        public double EndSeconds { get; set; }
    }
}