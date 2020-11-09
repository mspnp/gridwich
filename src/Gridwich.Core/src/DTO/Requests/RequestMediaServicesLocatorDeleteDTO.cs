using System;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Requests the deletion of a streaming locator.
    /// </summary>
    public sealed class RequestMediaServicesLocatorDeleteDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the locator name to delete.
        /// </summary>
        [JsonProperty("locatorName", Required = Required.Always)]
        public string LocatorName { get; set; }
    }
}