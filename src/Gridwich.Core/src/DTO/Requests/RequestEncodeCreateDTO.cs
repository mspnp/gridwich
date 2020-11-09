using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// RequestEncodeCreateDTO EventGrid data payload.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class RequestEncodeCreateDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets Input file lists.
        /// </summary>
        [JsonProperty("inputs")]
        public IEnumerable<InputItem> Inputs { get; set; }

        /// <summary>
        /// Gets or sets the container Url that will be used for output storage.
        /// </summary>
        [JsonProperty("outputContainer")]
        public string OutputContainer { get; set; }
    }

    /// <summary>
    /// Input Blob Uris (Urls actually), that are inputs to the encode process.
    /// </summary>
    public class InputItem
    {
        /// <summary>
        /// Gets or sets the Blob Url for an asset involved in the encode process.
        /// </summary>
        [property:SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification="Not applicable to DTOs")]
        [JsonProperty("blobUri")]
        public string BlobUri { get; set; }
    }
}