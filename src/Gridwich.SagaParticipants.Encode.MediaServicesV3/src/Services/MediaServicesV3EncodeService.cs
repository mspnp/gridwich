using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms;
using Gridwich.Services.Core.Exceptions;
using Microsoft.Azure.Management.Media.Models;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3
{
    /// <summary>
    /// Implementation of IMediaServicesV3Service.
    /// This class provides features leverage Media Services V3 for the encoding.
    /// </summary>
    public class MediaServicesV3EncodeService : MediaServicesV3BaseService, IMediaServicesV3EncodeService
    {
        private readonly IMediaServicesV3TransformService _mediaServicesV3TransformService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3EncodeService"/> class.
        /// </summary>
        /// <param name="mediaServicesV3SdkWrapper">Media Services V3 Wrapper object.</param>
        /// <param name="mediaServicesV3TransformService">Media Services V3 Transform Service.</param>
        /// <param name="log">Logger.</param>
        public MediaServicesV3EncodeService(
            IMediaServicesV3TransformService mediaServicesV3TransformService,
            IMediaServicesV3SdkWrapper mediaServicesV3SdkWrapper,
            IObjectLogger<MediaServicesV3BaseService> log)
            : base(mediaServicesV3SdkWrapper, log)
        {
            _mediaServicesV3TransformService = mediaServicesV3TransformService;
        }

        /// <summary>
        /// This function checks a list of parameters and report an exception if one is null or white space.
        /// </summary>
        /// <param name="param">Value of the parameter to check.</param>
        /// <param name="nameParam">The name of the parameter to check.</param>
        /// <exception cref="ArgumentNullException">Thrown when one parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when one parameter is empty or white space.</exception>
        [DebuggerStepThrough]
        private static void CheckArgumentNotNullOrEmpty(string param, string nameParam)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameParam);
            }
            else if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} parameter is empty or white space.", nameParam));
            }
        }

        /// <inheritdoc/>
        /// <param name="endTime">Mark Out of the encode.  If null or empty, will end at, well the end.</param>
        public async Task CreateJobAsync(string transformName, string inputAssetName, string ouputAssetName, string jobName, TimeBasedEncodeDTO timeBasedEncodeInfo, IDictionary<string, string> correlationData, JObject operationContext)
        {
            await ConnectAsync().ConfigureAwait(false);

            CheckArgumentNotNullOrEmpty(transformName, nameof(transformName));
            CheckArgumentNotNullOrEmpty(inputAssetName, nameof(inputAssetName));
            CheckArgumentNotNullOrEmpty(ouputAssetName, nameof(ouputAssetName));
            CheckArgumentNotNullOrEmpty(jobName, nameof(jobName));

            // let's build the input and output objects
            var jobInput = new JobInputAsset(inputAssetName);
            var jobOutputs = new[] { new JobOutputAsset(ouputAssetName) };

            // process mark in and mark out if provided, null otherwise.
            if (!(timeBasedEncodeInfo is null))
            {
                if (timeBasedEncodeInfo.StartSeconds < 0)
                {
                    throw new GridwichTimeParameterException(nameof(timeBasedEncodeInfo.StartSeconds), timeBasedEncodeInfo.StartSeconds, "Must be above zero.");
                }

                jobInput.Start = new AbsoluteClipTime(TimeSpan.FromSeconds(timeBasedEncodeInfo.StartSeconds));

                if (timeBasedEncodeInfo.EndSeconds < 0)
                {
                    throw new GridwichTimeParameterException(nameof(timeBasedEncodeInfo.EndSeconds), timeBasedEncodeInfo.EndSeconds, "Must be above zero.");
                }

                jobInput.End = new AbsoluteClipTime(TimeSpan.FromSeconds(timeBasedEncodeInfo.EndSeconds));
            }

            // Submit the job:
            _ = await this.MediaServicesV3SdkWrapper.JobCreateAsync(
                transformName: transformName,
                jobName: jobName,
                parameters: new Job
                {
                    Input = jobInput,
                    Outputs = jobOutputs,
                    CorrelationData = correlationData,
                }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task CreateTransformIfNotExistByNameAsync(string transformName, JObject operationContext)
        {
            await ConnectAsync().ConfigureAwait(false);

            CheckArgumentNotNullOrEmpty(transformName, nameof(transformName));

            try
            {
                var transform = await this.MediaServicesV3SdkWrapper.TransformGetAsync(transformName).ConfigureAwait(false);
            }
            catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Attempt to find the transform in the Media Services V3 Dictionary
                var mediaServicesV3Transform = _mediaServicesV3TransformService.GetTransform(transformName);

                // Check to see if the transform was located
                if (mediaServicesV3Transform == null)
                {
                    throw new Exception($"The Transform named {transformName} could not be located in the list of valid Transforms in Media Services V3.");
                }

                // In the future, may want to modify to accept the transform description
                // Create the transform
                await this.MediaServicesV3SdkWrapper.TransformCreateOrUpdateAsync(transformName, mediaServicesV3Transform.Output.TransformOutputs).ConfigureAwait(false);
            }
        }
    }
}