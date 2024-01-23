using Gridwich.Core.Constants;
using Newtonsoft.Json;
using System;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for ResponseStorageBlobDeletedEvent.
    /// </summary>
    public sealed class ResponseBlobDeleteSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the BlobUri of the target.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBlobDeleteSuccessDTO"/> class.
        /// </summary>
        public ResponseBlobDeleteSuccessDTO()
            : base(CustomEventTypes.ResponseBlobDeleteSuccess)
        {
        }
    }
}