using Gridwich.Core.Constants;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for ContainerDeletedEvent.
    /// </summary>
    public class ResponseContainerDeleteSuccessDTO : ResponseBaseDTO
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
        /// Initializes a new instance of the <see cref="ResponseContainerDeleteSuccessDTO"/> class.
        /// </summary>
        public ResponseContainerDeleteSuccessDTO()
            : base(CustomEventTypes.ResponseBlobContainerDeleteSuccess)
        {
        }
    }
}