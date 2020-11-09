using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class is a base class for all Gridwich DTOs.
    /// </summary>
    public class RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the OperationContext.
        /// </summary>
        [JsonProperty("operationContext")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for serialize/deserialize")]
        public JObject OperationContext { get; set; }
    }
}