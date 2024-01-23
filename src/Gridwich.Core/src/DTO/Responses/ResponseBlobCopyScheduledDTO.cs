using Gridwich.Core.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for a Gridwich BlobCopyScheduled event.
    /// </summary>
    public sealed class ResponseBlobCopyScheduledDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the Source BlobUri of the target.
        /// </summary>
        [JsonProperty("sourceUri")]
        public Uri SourceUri { get; set; }

        /// <summary>
        /// Gets or sets payload of the Metadata for the source.
        /// </summary>
        [JsonProperty("blobMetadata")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject BlobMetadata { get; set; }

        /// <summary>
        /// Gets or sets the Destination BlobUri of the target.
        /// </summary>
        [JsonProperty("destinationUri")]
        public Uri DestinationUri { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBlobCopyScheduledDTO"/> class.
        /// </summary>
        public ResponseBlobCopyScheduledDTO()
            : base(CustomEventTypes.ResponseBlobCopyScheduled)
        {
        }
    }
}