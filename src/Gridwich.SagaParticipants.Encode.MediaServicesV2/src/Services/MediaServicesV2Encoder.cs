using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Presets;
using Gridwich.Services.Core.Exceptions;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Services
{
    /// <summary>
    /// This is the AMS V2 Encoder
    /// </summary>
    public class MediaServicesV2Encoder : IMediaServicesV2Encoder
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IMediaServicesV2EncodeService _mediaServicesV2Service;
        private readonly IMediaServicesPreset _mediaServicesPreset;
        private readonly IObjectLogger<MediaServicesV2Encoder> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2Encoder"/> class.
        /// </summary>
        /// <param name="log">IObjectLogger log.</param>
        /// <param name="settingsProvider">ISettingsProvider settingsProvider.</param>
        /// <param name="mediaServicesV2Service">IMediaServicesV2Service.</param>
        /// <param name="mediaServicesPreset">IMediaServicesPreset.</param>
        public MediaServicesV2Encoder(IObjectLogger<MediaServicesV2Encoder> log,
            ISettingsProvider settingsProvider,
            IMediaServicesV2EncodeService mediaServicesV2Service,
            IMediaServicesPreset mediaServicesPreset)
        {
            _log = log;
            _settingsProvider = settingsProvider;
            _mediaServicesV2Service = mediaServicesV2Service;
            _mediaServicesPreset = mediaServicesPreset;
        }

        /// <summary>
        /// EncodeAsync does the heavy lifting to encode and process a video asset.
        /// </summary>
        /// <param name="encodeCreateDTO">encodeCreateDTO.</param>
        /// <returns>Returns true if encode request was successful, otherwise false.</returns>
        public async Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestMediaServicesV2EncodeCreateDTO encodeCreateDTO)
        {
            _ = encodeCreateDTO ?? throw new ArgumentNullException(nameof(encodeCreateDTO));

            List<Uri> sourceUris = new List<Uri>();
            try
            {
                sourceUris = encodeCreateDTO.Inputs.ToList().Select(u => new Uri(u.BlobUri)).ToList();
            }
            catch (Exception e)
            {
                throw new GridwichEncodeCreateJobException($"Failed to parse Inputs", encodeCreateDTO.OperationContext, e, LogEventIds.MediaServicesV2InputError);
            }

            string inputAssetId = await _mediaServicesV2Service.CopyFilesIntoNewAsset(sourceUris).ConfigureAwait(false);

            var presetName = encodeCreateDTO.PresetName;
            TimeSpan? thumbnailTimeSpan;
            if (encodeCreateDTO.ThumbnailTimeSeconds == 0)
            {
                thumbnailTimeSpan = null;
            }
            else
            {
                if (encodeCreateDTO.ThumbnailTimeSeconds < 0)
                {
                    throw new GridwichTimeParameterException(nameof(encodeCreateDTO.ThumbnailTimeSeconds), encodeCreateDTO.ThumbnailTimeSeconds, "Must be above zero.");
                }

                thumbnailTimeSpan = TimeSpan.FromSeconds(encodeCreateDTO.ThumbnailTimeSeconds);
            }


            var preset = _mediaServicesPreset.GetPresetForPresetName(presetName, thumbnailTimeSpan);
            if (string.IsNullOrEmpty(preset))
            {
                throw new GridwichEncodeCreateJobException($"Failed for PresetName {presetName}", encodeCreateDTO.OperationContext, null, LogEventIds.MediaServicesV2PresetError);
            }

            string jobId;
            try
            {
                Uri callbackEndpoint = new Uri(_settingsProvider.GetAppSettingsValue("AmsV2CallbackEndpoint"));
                var outputContainer = new Uri(encodeCreateDTO.OutputContainer);
                var correlationData = new Dictionary<string, string>()
                     {
                            { "outputAssetContainer", outputContainer.ToString() },
                            { "operationContext", encodeCreateDTO.OperationContext.ToString() },
                     };

                jobId = await _mediaServicesV2Service.SubmitMesJobAsync(
                    inputAssetId,
                    preset,
                    outputContainer,
                    callbackEndpoint,
                    correlationData).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new GridwichEncodeCreateJobException($"Failed to create job.", encodeCreateDTO.OperationContext, e, LogEventIds.MediaServicesV2CreateJobError);
            }

            _log.LogEvent(LogEventIds.MediaServicesV2JobSubmitCalled, jobId);

            return new ServiceOperationResultEncodeDispatched(
                workflowJobName: jobId,
                null,
                encodeCreateDTO.OperationContext);
        }
    }
}