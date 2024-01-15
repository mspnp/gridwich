using Gridwich.Core.Constants;
using Newtonsoft.Json;
using System;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for the ResponseBlobSASURLSuccess event.
    /// </summary>
    public sealed class ResponseBlobSasUrlSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the sasUrl of the target.
        /// </summary>
        [JsonProperty("sasUrl")]
        public Uri SasUrl { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBlobSasUrlSuccessDTO"/> class.
        /// </summary>
        public ResponseBlobSasUrlSuccessDTO()
            : base(CustomEventTypes.ResponseBlobSasUrlSuccess)
        {
        }
    }
}