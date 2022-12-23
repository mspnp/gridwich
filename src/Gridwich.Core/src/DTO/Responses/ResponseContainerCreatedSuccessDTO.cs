using System;
using Gridwich.Core.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for ContainerCreatedEvent.
    /// </summary>
    public class ResponseContainerCreatedSuccessDTO : ResponseBaseDTO
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
        /// Initializes a new instance of the <see cref="ResponseContainerCreatedSuccessDTO"/> class.
        /// </summary>
        /// <param name="eventType">eventType</param>
        public ResponseContainerCreatedSuccessDTO()
            : base(CustomEventTypes.ResponseBlobContainerSuccess)
        {
        }
    }
}