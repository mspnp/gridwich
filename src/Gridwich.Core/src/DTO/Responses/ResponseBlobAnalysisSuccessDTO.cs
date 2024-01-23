using Gridwich.Core.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// The results of an Analysis.
    /// </summary>
    public class ResponseBlobAnalysisSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the blobUri to analyze.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }

        /// <summary>
        /// Gets or sets the BlobMetadata of the target, if available.
        /// </summary>
        [JsonProperty("blobMetadata")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject BlobMetadata { get; set; }

        /// <summary>
        /// Gets or sets AnalysisResult.
        /// </summary>
        [JsonProperty("analysisResult")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject AnalysisResult { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBlobAnalysisSuccessDTO"/> class.
        /// </summary>
        public ResponseBlobAnalysisSuccessDTO()
            : base(CustomEventTypes.ResponseBlobAnalysisSuccess)
        {
        }
    }
}