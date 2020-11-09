using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for CreateMetadataEvent.
    /// </summary>
    public sealed class RequestBlobMetadataCreateDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the BlobUri of the source.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }

        /// <summary>
        /// Gets or sets payload of the Metadata for the source.
        /// </summary>
        [JsonProperty("blobMetadata")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject BlobMetadata { get; set; }
    }
}