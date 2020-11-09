using System;

using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Requestor-specific message for the BlobSASURLCreate event.
    /// </summary>
    public sealed class RequestBlobSasUrlCreateDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the BlobUri of the target.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }

        /// <summary>
        /// Gets or sets the secToLive of the target.
        /// </summary>
        [JsonProperty("secToLive")]
        public int SecToLive { get; set; }
    }
}
