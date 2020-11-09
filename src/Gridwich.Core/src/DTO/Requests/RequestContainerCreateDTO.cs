using System;

using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Requestor-specific message for the BlobContainerCreate event.
    /// </summary>
    public sealed class RequestContainerCreateDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the StorageAccountName of the target.
        /// </summary>
        [JsonProperty("storageAccountName")]
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the ContainerName of the target.
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; }
    }
}
