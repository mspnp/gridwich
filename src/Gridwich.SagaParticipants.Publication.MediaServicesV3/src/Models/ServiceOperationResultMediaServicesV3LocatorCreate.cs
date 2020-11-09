using System;

using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;

using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Models
{
    /// <summary>
    /// This type is return by the <see cref="IMediaServicesV3PublicationService"/> when calling LocatorCreate.
    /// </summary>
    public class ServiceOperationResultMediaServicesV3LocatorCreate : ServiceOperationResultBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceOperationResultMediaServicesV3LocatorCreate"/> class.
        /// </summary>
        /// <param name="locatorName">LocatorName which was published.</param>
        /// <param name="cencKeyId">The cencKeyId for the locator.</param>
        /// <param name="cbcsKeyId">The cbcsKeyId for the locator.</param>
        /// <param name="dashUri">The DASH uri for the locator.</param>
        /// <param name="hlsUri">The HLS uri for the locator.</param>
        /// <param name="operationContext">The OperationContext that triggered this service call.</param>
        public ServiceOperationResultMediaServicesV3LocatorCreate(
            string locatorName,
            string cencKeyId,
            string cbcsKeyId,
            Uri dashUri,
            Uri hlsUri,
            JObject operationContext)
            : base(operationContext)
        {
            LocatorName = locatorName;
            CENCKeyId = cencKeyId;
            CBCSKeyId = cbcsKeyId;
            DashUri = dashUri;
            HlsUri = hlsUri;
        }

        /// <summary>
        /// Gets locatorName which was published.
        /// </summary>
        public string LocatorName { get; }

        /// <summary>
        /// Gets the cencKeyId which was used for Widevine and PlayReady.
        /// </summary>
        public string CENCKeyId { get; }

        /// <summary>
        /// Gets the cbcsKeyId which was used for FairPlay.
        /// </summary>
        public string CBCSKeyId { get; }

        /// <summary>
        /// Gets the DASH uri for the locator.
        /// </summary>
        public Uri DashUri { get; }

        /// <summary>
        /// Gets the HLS uri for the locator.
        /// </summary>
        public Uri HlsUri { get; }
    }
}