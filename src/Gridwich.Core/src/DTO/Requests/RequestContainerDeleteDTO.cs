using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for ContainerDeletedEvent.
    /// </summary>
    public class RequestContainerDeleteDTO : RequestBaseDTO
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
    }
}