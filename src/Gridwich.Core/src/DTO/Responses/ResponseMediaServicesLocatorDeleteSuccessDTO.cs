using System;
using Gridwich.Core.Constants;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Deletion of a streaming locator completion message.
    /// </summary>
    public sealed class ResponseMediaServicesLocatorDeleteSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the locatorName which was deleted.
        /// </summary>
        [JsonProperty("locatorName")]
        public string LocatorName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMediaServicesLocatorDeleteSuccessDTO"/> class.
        /// </summary>
        public ResponseMediaServicesLocatorDeleteSuccessDTO()
            : base(CustomEventTypes.ResponseMediaservicesLocatorDeleteSuccess)
        {
        }
    }
}