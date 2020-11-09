using System;
using Gridwich.Core.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for the ResponseBlobDeleteScheduled event.
    /// </summary>
    public sealed class ResponseBlobDeleteScheduledDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the BlobUri of the target.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }

        /// <summary>
        /// Gets or sets payload of the Metadata for the target.
        /// </summary>
        [JsonProperty("blobMetadata")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject BlobMetadata { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBlobDeleteScheduledDTO"/> class.
        /// </summary>
        public ResponseBlobDeleteScheduledDTO()
            : base(CustomEventTypes.ResponseBlobDeleteScheduled)
        {
        }
    }
}