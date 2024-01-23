using Newtonsoft.Json;
using System.Collections.Generic;

namespace Gridwich.Core.Models
{
    /// <summary>
    /// Allows the caller to specify the options used for the .Inform() call in MediaInfo.
    /// </summary>
    [JsonObject(ItemRequired = Required.Always)]
    public class MediaInfoAnalyzerSpecificData
    {
        /// <summary>
        /// Gets or sets CommandLineOptions.
        /// For each of these key-value pairs, MediaInfo.Options(key,value) will be called.
        /// For example:
        ///   Complete, 1
        ///   Output, JSON
        /// would be expected.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public IDictionary<string, string> CommandLineOptions { get; set; }
    }
}
