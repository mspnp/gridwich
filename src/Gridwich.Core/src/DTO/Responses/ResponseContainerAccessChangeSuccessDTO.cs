using Gridwich.Core.Constants;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for the ResponseContainerAccessChangeSuccess event.
    /// </summary>
    public class ResponseContainerAccessChangeSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the StorageAccountName of the target, if available.
        /// </summary>
        [JsonProperty("storageAccountName")]
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the ContainerName of the target, if available.
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; }

        /// <summary>
        /// Gets or sets the AccessType of the target, if available.
        /// </summary>
        [JsonProperty("accessType")]
        public ContainerAccessType AccessType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseContainerAccessChangeSuccessDTO"/> class.
        /// </summary>
        /// <param name="eventType">eventType</param>
        public ResponseContainerAccessChangeSuccessDTO()
            : base(CustomEventTypes.ResponseBlobContainerAccessChangeSuccess)
        {
        }
    }
}