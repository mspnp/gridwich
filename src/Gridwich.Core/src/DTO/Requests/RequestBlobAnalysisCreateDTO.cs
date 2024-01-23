using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Contains the request details about how to analyze a file.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public sealed class RequestBlobAnalysisCreateDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the blobUri to analyze.
        /// </summary>
        [JsonProperty("blobUri")]
        public Uri BlobUri { get; set; }

        /// <summary>
        /// Gets or sets AnalyzerSpecificData.
        /// See MediaInfoAnalyzerSpecificData.
        /// </summary>
        [JsonProperty("analyzerSpecificData")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject AnalyzerSpecificData { get; set; }
    }
}