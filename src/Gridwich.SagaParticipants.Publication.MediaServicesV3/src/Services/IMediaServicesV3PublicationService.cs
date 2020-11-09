using System;
using System.Threading.Tasks;
using Gridwich.Core.DTO;
using Gridwich.Core.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Services
{
    /// <summary>
    /// Manages the Azure Media Services publication operations.
    /// </summary>
    public interface IMediaServicesV3PublicationService
    {
        /// <summary>
        /// Publishes the container by creating a locator using the specified protection and filters.
        /// </summary>
        /// <param name="containerUri">The containerUri to publish.</param>
        /// <param name="streamingPolicy">The PredefinedStreamingPolicy, or a custom name.</param>
        /// <param name="contentKeyPolicy">The ContentKeyPolicy name.</param>
        /// <param name="timeBasedFilterInfo">Specifies filter createion if needed.</param>
        /// <param name="operationContext">The OperationContext of the request.</param>
        /// <param name="generateAudioFilters">Generate or not the audio filters.</param>
        /// <returns>ServiceOperationResultMediaServicesV3Publish including locatorName, dash uri and hls uri.</returns>
        /// <exception cref="GridwichMediaServicesV3ConnectivityException">If there are connetivity issues with MediaServicesV3.</exception>
        /// <exception cref="GridwichPublicationStreamingPolicyNotSupportedException">If the requested protection is not supported or implemented.</exception>
        public Task<ServiceOperationResultMediaServicesV3LocatorCreate> LocatorCreateAsync(Uri containerUri, string streamingPolicy, string contentKeyPolicy, TimeBasedFilterDTO timeBasedFilterInfo, JObject operationContext, bool generateAudioFilters);


        /// <summary>
        /// Deletes the specified locator.
        /// </summary>
        /// <param name="locatorName">The locatorName.</param>
        /// <param name="operationContext">The OperationContext of the request.</param>
        /// <returns>Returning the OperationContext signals that the operation was successful.  Otherwise, the service will throw.</returns>
        /// <exception cref="GridwichMediaServicesV3ConnectivityException">If there are connetivity issues with MediaServicesV3.</exception>
        /// <exception cref="GridwichPublicationLocatorCreationException">If the requested locatorName is not found.</exception>
        public Task<ServiceOperationResultMediaServicesV3LocatorDelete> LocatorDeleteAsync(string locatorName, JObject operationContext);
    }
}
