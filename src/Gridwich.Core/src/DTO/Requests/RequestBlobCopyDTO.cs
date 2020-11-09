using System;

using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Requestor-specific message for the BlobCopy event.
    /// </summary>
    public sealed class RequestBlobCopyDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the Source BlobUri of the target.
        /// </summary>
        [JsonProperty("sourceUri")]
        public Uri SourceUri { get; set; }

        /// <summary>
        /// Gets or sets the Destination BlobUri of the target.
        /// </summary>
        [JsonProperty("destinationUri")]
        public Uri DestinationUri { get; set; }
    }
}