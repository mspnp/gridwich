using System;
using Gridwich.Core.Constants;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// Creation of a streaming locator completion message.
    /// </summary>
    public sealed class ResponseMediaServicesLocatorCreateSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the locatorName which was published.
        /// </summary>
        [JsonProperty("locatorName")]
        public string LocatorName { get; set; }

        /// <summary>
        /// Gets or sets the cencKeyId which was used for Widevine and PlayReady.
        /// </summary>
        [JsonProperty("cencKeyId")]
        public string CENCKeyId { get; set; }

        /// <summary>
        /// Gets or sets the cbcsKeyId which was used for FairPlay.
        /// </summary>
        [JsonProperty("cbcsKeyId")]
        public string CBCSKeyId { get; set; }

        /// <summary>
        /// Gets or sets the DASH uri for the locator.
        /// </summary>
        [JsonProperty("dashUri")]
        public Uri DashUri { get; set; }

        /// <summary>
        /// Gets or sets the HLS uri for the locator.
        /// </summary>
        [JsonProperty("hlsUri")]
        public Uri HlsUri { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMediaServicesLocatorCreateSuccessDTO"/> class.
        /// </summary>
        public ResponseMediaServicesLocatorCreateSuccessDTO()
            : base(CustomEventTypes.ResponseMediaservicesLocatorCreateSuccess)
        {
        }
    }
}