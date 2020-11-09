using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3.Exceptions;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3
{
    /// <summary>
    /// This is the implementation of the Media Services V3 encoder.
    /// </summary>
    public class MediaServicesV3Encoder : IMediaServicesV3Encoder
    {
        private readonly IMediaServicesV3EncodeService _amsV3Service;
        private readonly IStorageService _storageService;
        private readonly IObjectLogger<MediaServicesV3Encoder> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3Encoder"/> class.
        /// </summary>
        /// <param name="log">IObjectLogger log.</param>
        /// <param name="storageService">IStorageService.</param>
        /// <param name="amsV3Service">IMediaServicesOperationsService provider.</param>
        public MediaServicesV3Encoder(IObjectLogger<MediaServicesV3Encoder> log,
            IStorageService storageService,
            IMediaServicesV3EncodeService amsV3Service)
        {
            _log = log;
            _storageService = storageService;
            _amsV3Service = amsV3Service;
        }

        /// <summary>
        /// EncodeCreateAsync does the heavy lifting to encode and process a video asset in Azure Media Services V3.
        /// </summary>
        /// <param name="encodeCreateDTO">Data payload for creating an AMS V3 encode.</param>
        /// <returns>Returns true if encode request was successful, otherwise false.</returns>
        public async Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestMediaServicesV3EncodeCreateDTO encodeCreateDTO)
        {
            _ = encodeCreateDTO ?? throw new ArgumentNullException(nameof(encodeCreateDTO));

            // 1. Get or create the transform
            // 2. Create input asset
            // 3. Create output asset
            // 4. Call the encode
            // 5. Confirm success to base class

            var operationContext = encodeCreateDTO.OperationContext;

            // 1. Get or create the transform
            try
            {
                if (string.IsNullOrWhiteSpace(encodeCreateDTO.TransformName))
                {
                    throw new ArgumentOutOfRangeException(nameof(encodeCreateDTO), "TransformName can not be null or whitespace");
                }
            }
            catch (Exception e)
            {
                throw new GridwichEncodeCreateDataException("Invalid inputs found in EncodeCreateDTO.", encodeCreateDTO.OperationContext, e, LogEventIds.MediaServicesV3EncodeCreateDtoError);
            }

            try
            {
                await _amsV3Service.CreateTransformIfNotExistByNameAsync(encodeCreateDTO.TransformName, operationContext).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new GridwichMediaServicesV3CreateTransformException($"Error creating AMS V3 Transform: {encodeCreateDTO.TransformName}", LogEventIds.MediaServicesV3TransformError, operationContext, e);
            }

            // 2. Create input asset
            var sourceUris = Enumerable.Empty<Uri>();
            try
            {
                sourceUris = encodeCreateDTO.Inputs.Select(u => new Uri(u.BlobUri, UriKind.Absolute));
                if (!sourceUris.Any() || sourceUris.Count() != encodeCreateDTO.Inputs.Count())
                {
                    throw new ArgumentException("Could not parse Inputs.", nameof(encodeCreateDTO));
                }

                // Check that all the blobs actually exist.

                // Create a new muted context for these operations, based on the Op context if available.
                var internalCorrelator = (operationContext != null) ? operationContext.DeepClone() as JObject : new JObject();
                internalCorrelator.Add("~AMS-V3-Encode", $"G:{Guid.NewGuid()}");
                var context = new StorageClientProviderContext(internalCorrelator, muted: true);

                foreach (var blobUri in sourceUris)
                {
                    var exists = await _storageService.GetBlobExistsAsync(blobUri, context).ConfigureAwait(false);
                    if (!exists)
                    {
                        _log.LogEventObject(LogEventIds.MediaServicesV3AttemptToUseNonexistentBlob, blobUri);
                        throw new GridwichMediaServicesV3Exception($"Attempt to use nonexistent blob as input: {blobUri}",
                            LogEventIds.MediaServicesV3AttemptToUseNonexistentBlob, context.ClientRequestIdAsJObject);
                    }
                }
            }
            catch (Exception e) when (!(e is GridwichMediaServicesV3Exception))
            {
                throw new GridwichEncodeCreateDataException("Invalid inputs found in EncodeCreateDTO.", encodeCreateDTO.OperationContext, e, LogEventIds.MediaServicesV3EncodeCreateDtoError);
            }

            string inputAssetName = null;
            try
            {
                inputAssetName = await _amsV3Service.CreateOrUpdateAssetForContainerAsync(sourceUris).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new GridwichMediaServicesV3CreateAssetException("Error creating/getting AMS V3 Asset.", LogEventIds.MediaServicesV3InputAssetError, operationContext, e);
            }

            // 4. Create output asset.
            Uri outputUri = null;
            try
            {
                outputUri = new Uri(encodeCreateDTO.OutputContainer, UriKind.Absolute);
            }
            catch (Exception e)
            {
                throw new GridwichEncodeInvalidOutputContainerException($"Invalid output container specified: {encodeCreateDTO.OutputContainer}", operationContext, e, LogEventIds.MediaServicesV3OutputError);
            }

            string outputAssetName = null;
            try
            {
                outputAssetName = await _amsV3Service.CreateOrUpdateAssetForContainerAsync(Enumerable.Empty<Uri>().Append(outputUri)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new GridwichMediaServicesV3CreateAssetException($"Error creating/getting AMS V3 output Asset: {outputUri}.", LogEventIds.MediaServicesV3OutputError, operationContext, e);
            }

            // 5. Execute Encode
            var uniqueness = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 11);
            var jobName = outputAssetName + "-" + uniqueness;
            try
            {
                await _amsV3Service.CreateJobAsync(encodeCreateDTO.TransformName,
                                                   inputAssetName,
                                                   outputAssetName,
                                                   jobName,
                                                   encodeCreateDTO.TimeBasedEncode,
                                                   new Dictionary<string, string>()
                                                   {
                                                       { "mediaServicesV3EncoderSpecificData", JsonConvert.SerializeObject(encodeCreateDTO) },
                                                       { "inputAssetName", inputAssetName },
                                                       { "outputAssetContainer", encodeCreateDTO.OutputContainer },
                                                       { "outputAssetName", outputAssetName },
                                                       { "operationContext", encodeCreateDTO.OperationContext.ToString() }
                                                   },
                                                   operationContext).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new GridwichEncodeCreateJobException($"Error creating AMS V3 job: {jobName}", operationContext, e, LogEventIds.MediaServicesV3CreateJobApiError);
            }

            // 6. Confirm success to base class
            _log.LogEvent(LogEventIds.MediaServicesV3JobSubmitCalled, $"AMS V3 job successfully logged: {jobName}");
            return new ServiceOperationResultEncodeDispatched(
                workflowJobName: jobName,
                null,
                encodeCreateDTO.OperationContext);
        }
    }
}