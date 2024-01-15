using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.Flip.Models;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telestream.Cloud.Flip.Client;
using Telestream.Cloud.Flip.Model;

namespace Gridwich.SagaParticipants.Encode.Flip.Services
{
    /// <summary>
    /// Flip implementation of the Encoder class.
    /// A simple version of the Telestream Vantage Cloud Port API.
    /// </summary>
    public class FlipService : IFlipService
    {
        private readonly ITelestreamCloudClientProvider _telestreamCloudClientProvider;
        private readonly ITelestreamCloudStorageProvider _telestreamCloudStorageProvider;
        private readonly IStorageService _storageService;
        private readonly TimeSpan _defaultTTL = TimeSpan.FromHours(6);

        /// <summary>
        /// Initializes a new instance of the <see cref="FlipService"/> class.
        /// </summary>
        /// <param name="storageService">IStorageService storageService.</param>
        /// <param name="telestreamCloudClientProvider">Client for Telestream API calls.</param>
        /// <param name="telestreamCloudStorageProvider">Storage Reference services for Telestream.</param>
        public FlipService(
            IStorageService storageService,
            ITelestreamCloudClientProvider telestreamCloudClientProvider,
            ITelestreamCloudStorageProvider telestreamCloudStorageProvider)
        {
            _storageService = storageService;
            _telestreamCloudClientProvider = telestreamCloudClientProvider;
            _telestreamCloudStorageProvider = telestreamCloudStorageProvider;
        }

        /// <inheritdoc/>
        public async Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestFlipEncodeCreateDTO requestorFlipEncodeCreateDTO)
        {
            _ = requestorFlipEncodeCreateDTO ?? throw new ArgumentNullException(nameof(requestorFlipEncodeCreateDTO));

            TimeSpan ttl = requestorFlipEncodeCreateDTO.SecToLive == 0 ? _defaultTTL : TimeSpan.FromSeconds(requestorFlipEncodeCreateDTO.SecToLive);
            var inputs = requestorFlipEncodeCreateDTO.Inputs.ToArray();
            var context = new StorageClientProviderContext(requestorFlipEncodeCreateDTO.OperationContext);
            var input = new Uri(inputs[0].BlobUri);

            // EncodeAsync is broken into 2 parts
            //  1. Configure any storage needs for the encoder
            //  2. Call the encoder

            // 1. configure storage for encoder

            // 1a. Input must exist
            var exists = await _storageService.GetBlobExistsAsync(input, context).ConfigureAwait(false);
            if (!exists)
            {
                throw new GridwichFlipMissingInputException(
                        $"Attempt to use nonexistent blob as input: {input}",
                        input.AbsoluteUri, context.ClientRequestIdAsJObject);
            }

            // 1b. SAS URI needed for input.
            string sasUri;
            try
            {
                sasUri = _storageService.GetSasUrlForBlob(input, ttl, context);
                if (string.IsNullOrEmpty(sasUri))
                {
                    throw new GridwichFlipSASException($"Failed to generate SAS for: {input}", requestorFlipEncodeCreateDTO.OperationContext);
                }
            }
            catch (Exception e)
            {
                throw new GridwichFlipSASException($"Failed to generate SAS for: {input}", requestorFlipEncodeCreateDTO.OperationContext, e);
            }

            // 2. Execute Encode
            var result = await CreateVideoAsync(sasUri, requestorFlipEncodeCreateDTO).ConfigureAwait(false);

            return new ServiceOperationResultEncodeDispatched(
                workflowJobName: result.Id,
                null,
                requestorFlipEncodeCreateDTO.OperationContext);
        }

        /// <summary>
        /// Preps and calls the Flip API to encode a video.
        /// </summary>
        /// <param name="sasUri">Generated asset Uri with SAS token.</param>
        /// <param name="requestorFlipEncodeCreateDTO">Encode specific data object.</param>
        ///
        /// <returns>A "Video" object.</returns>
        private async Task<Video> CreateVideoAsync(string sasUri, RequestFlipEncodeCreateDTO requestorFlipEncodeCreateDTO)
        {
            // Get or create the TelestreamCloud storage reference to our output container.
            var store = await _telestreamCloudStorageProvider.GetStoreByNameAsync(new Uri(requestorFlipEncodeCreateDTO.OutputContainer)).ConfigureAwait(false);

            var newVideo = new CreateVideoBody
            {
                Profiles = requestorFlipEncodeCreateDTO.Profiles,
                SourceUrl = sasUri,
                StoreId = store.Id
            };

            var factory = await GetFactoryByNameAsync(requestorFlipEncodeCreateDTO.FactoryName).ConfigureAwait(false);

            // configure the encode payload for Correlation Vector
            var payload = new FlipPayload()
            {
                OperationContext = requestorFlipEncodeCreateDTO.OperationContext,
                FactoryId = factory.Id,
                OutputContainer = requestorFlipEncodeCreateDTO.OutputContainer
            };

            newVideo.Payload = JsonConvert.SerializeObject(payload);

            try
            {
                var video = await _telestreamCloudClientProvider.FlipApi.CreateVideoAsync(factory.Id, newVideo).ConfigureAwait(false);
                return video;
            }
            catch (ApiException ae)
            {
                throw new GridwichFlipApiException("Error calling CreateVideoAsync.", requestorFlipEncodeCreateDTO.OperationContext, ae);
            }
        }

        /// <summary>
        /// Gets detailed info about an encoder job.
        /// </summary>
        /// <param name="flipEncodingCompleteData">Encode complete data to get Ids to find the encoding information.</param>
        /// <returns>Returns a Telestream Flip Encoding object. Returns null if not found.</returns>
        public Encoding GetEncodeInfo(FlipEncodingCompleteData flipEncodingCompleteData)
        {
            _ = flipEncodingCompleteData ?? throw new ArgumentNullException(nameof(flipEncodingCompleteData));
            var encoding = _telestreamCloudClientProvider.FlipApi.GetEncoding(flipEncodingCompleteData.EncodingId, flipEncodingCompleteData.VideoPayload.FactoryId);
            return encoding;
        }

        private async Task<Factory> GetFactoryByNameAsync(string factoryName)
        {
            var factories = await _telestreamCloudClientProvider.FlipApi.ListFactoriesAsync().ConfigureAwait(false);
            var factory = factories.Factories.FirstOrDefault(w => w.Name == factoryName);
            return factory ?? throw new GridwichFlipFactoryDoesNotExistException($"Factory not found: {factoryName}", null);
        }
    }
}