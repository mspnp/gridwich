using System;

using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Requests the deletion of a file.
    /// </summary>
    public sealed class RequestBlobDeleteDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the blobUri to delete.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }
    }
}