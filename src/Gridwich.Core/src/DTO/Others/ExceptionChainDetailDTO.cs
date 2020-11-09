using System.Collections;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Exception Details used in GridwichFailureDTO.
    /// </summary>
    public class ExceptionChainDetailDTO
    {
        /// <summary>
        /// Exception data.
        /// </summary>
        [property:System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Not applicable to DTOs")]
        [JsonProperty("data")]
        public IDictionary Data { get; set; }

        /// <summary>
        /// Exception message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
