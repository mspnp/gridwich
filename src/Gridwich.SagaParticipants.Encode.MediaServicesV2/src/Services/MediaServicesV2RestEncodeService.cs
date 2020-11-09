using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Services
{
    /// <summary>
    /// Implements the services that performs work using AMS V2 REST calls.
    /// </summary>
    public class MediaServicesV2RestEncodeService : IMediaServicesV2EncodeService
    {
        private readonly IObjectLogger<MediaServicesV2RestEncodeService> _log;
        private readonly IStorageService _storageService;
        private readonly IMediaServicesV2RestWrapper _mediaServicesV2RestWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2RestEncodeService"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="mediaServicesV2RestWrapper">mediaServicesV2RestWrapper.</param>
        public MediaServicesV2RestEncodeService(IObjectLogger<MediaServicesV2RestEncodeService> log, IStorageService storageService, IMediaServicesV2RestWrapper mediaServicesV2RestWrapper)
        {
            _log = log;
            _storageService = storageService;
            _mediaServicesV2RestWrapper = mediaServicesV2RestWrapper;
        }

        /// <inheritdoc/>
        public async Task<string> CopyFilesIntoNewAsset(IEnumerable<Uri> filesToCopy)
        {
            _ = filesToCopy ?? throw new ArgumentNullException(nameof(filesToCopy));
            _ = !filesToCopy.Any() ? throw new ArgumentOutOfRangeException(nameof(filesToCopy), "Count is zero") : 0;

            string newAssetId;
            var assetUriBuilder = new BlobUriBuilder(filesToCopy.First());
            string assetName = GetInputAssetName(assetUriBuilder);
            string assetAccountName = assetUriBuilder.AccountName;
            Uri assetUri;
            try
            {
                (newAssetId, assetUri) = await _mediaServicesV2RestWrapper.CreateEmptyAssetAsync(assetName, assetAccountName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2InputAssetError, e, assetUriBuilder.ToUri());
                throw new GridwichEncodeCreateJobException($"Failed to create asset for {assetUriBuilder.ToUri()}", null, e, LogEventIds.MediaServicesV2InputAssetError);
            }
            _log.LogEventObject(LogEventIds.MediaServicesV2AssetCreated, new { newAssetId, assetName });

            try
            {
                // Create a new muted context for these copy operations
                var internalCorrelator = new JObject();
                string id = (!string.IsNullOrWhiteSpace(newAssetId)) ? newAssetId : $"G:{Guid.NewGuid()}";
                internalCorrelator.Add("~AMS-V2-Encode", id);
                var context = new StorageClientProviderContext(internalCorrelator, muted: true);

                foreach (var fileToCopy in filesToCopy)
                {
                    var sourceUriBuilder = new BlobUriBuilder(fileToCopy);
                    var destUriBuilder = new BlobUriBuilder(assetUri)
                    {
                        // we need to remove subfolders if any, as AMS v2 does not support subfolder(s) in an asset container
                        BlobName = sourceUriBuilder.BlobName.Split('/').Last(),
                    };
                    var exists = await _storageService.GetBlobExistsAsync(fileToCopy, context).ConfigureAwait(false);
                    if (!exists)
                    {
                        _log.LogEventObject(LogEventIds.MediaServicesV2AttemptToUseNonexistentBlobAsInput, fileToCopy);
                        throw new GridwichMediaServicesV2Exception($"Attempted to use nonexistent blob: {fileToCopy} as input to encoding.", LogEventIds.MediaServicesV2AttemptToUseNonexistentBlobAsInput, context.ClientRequestIdAsJObject);
                    }
                    var s = new Stopwatch();
                    s.Start();
                    var copyFromUriOperation = await _storageService.BlobCopy(fileToCopy, destUriBuilder.ToUri(), context).ConfigureAwait(false);
                    var response = await copyFromUriOperation.WaitForCompletionAsync().ConfigureAwait(false);
                    s.Stop();
                    _log.LogEventObject(LogEventIds.MediaServicesV2CopyFileCompleted, new { CopyElapsedMilliseconds = s.ElapsedMilliseconds.ToString("G", CultureInfo.InvariantCulture) });
                }

                await _mediaServicesV2RestWrapper.CreateFileInfosAsync(newAssetId).ConfigureAwait(false);
                _log.LogEventObject(LogEventIds.MediaServicesV2CopyFileAndUpdateAssetSuccess, new { assetName, assetUri });
            }
            catch (Exception e) when (!(e is GridwichMediaServicesV2Exception))
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2CopyFileAndUpdateAssetError, e, filesToCopy);
                throw new GridwichEncodeCreateJobException($"Failed to copy {assetName} to {newAssetId}", null, e, LogEventIds.MediaServicesV2CopyFileAndUpdateAssetError);
            }

            return newAssetId;
        }

        /// <inheritdoc/>
        public async Task<string> SubmitMesJobAsync(string inputAssetId, string preset, Uri outputContainer, Uri callbackEndpoint, IDictionary<string, string> correlationData)
        {
            if (string.IsNullOrWhiteSpace(inputAssetId))
            {
                throw new ArgumentException($@"{nameof(inputAssetId)} is invalid", nameof(inputAssetId));
            }

            if (string.IsNullOrWhiteSpace(preset))
            {
                throw new ArgumentException($@"{nameof(preset)} is invalid", nameof(preset));
            }

            _ = outputContainer ?? throw new ArgumentNullException(nameof(outputContainer));

            _ = callbackEndpoint ?? throw new ArgumentNullException(nameof(callbackEndpoint));

            _ = correlationData ?? throw new ArgumentNullException(nameof(correlationData));

            string outputAssetName;
            string outputAssetStorageAccountName;
            string jobName;
            try
            {
                var outputContainerUriBuilder = new BlobUriBuilder(outputContainer);
                outputAssetStorageAccountName = outputContainerUriBuilder.AccountName;
                outputAssetName = GetOutputAssetName(outputAssetStorageAccountName, outputContainerUriBuilder);
                jobName = GenerateJobName(outputAssetName);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2FailedToParseOutputContainer, e, outputContainer);
                throw new GridwichEncodeCreateJobException($"Could not define output asset name or job name from {outputContainer}.", null, e, LogEventIds.MediaServicesV2FailedToParseOutputContainer);
            }

            string processorId;
            try
            {
                processorId = await _mediaServicesV2RestWrapper.GetLatestMediaProcessorAsync("Media Encoder Standard").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2FailedToGetProcessor, e, outputContainer);
                throw new GridwichEncodeCreateJobException($"Could not get media processor.", null, e, LogEventIds.MediaServicesV2FailedToGetProcessor);
            }

            string base64UrlEncodedCorrelationDataJsonString;
            try
            {
                var correlationDataJsonString = JsonConvert.SerializeObject(correlationData);
                base64UrlEncodedCorrelationDataJsonString = Base64UrlEncoder.Encode(correlationDataJsonString);
                if (base64UrlEncodedCorrelationDataJsonString.Length > 4000)
                {
                    const string ErrorMsg = "UrlEncoded and serialized correlationData is larger than 4000";
                    _log.LogEvent(LogEventIds.MediaServicesV2CorrelationDataError, ErrorMsg, correlationData);
                    throw new ArgumentException(ErrorMsg, nameof(correlationData));
                }
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2CorrelationDataError, e, correlationData);
                throw new GridwichEncodeCreateJobException($"Could not convert correlationData.", null, e, LogEventIds.MediaServicesV2CorrelationDataError);
            }

            string notificationEndPointId;
            try
            {
                notificationEndPointId = await _mediaServicesV2RestWrapper.GetOrCreateNotificationEndPointAsync("AmsV2Callback", callbackEndpoint).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2SpecificDataError, e, callbackEndpoint);
                throw new GridwichEncodeCreateJobException($"Could not create notification endpoint for {callbackEndpoint}", null, e, LogEventIds.MediaServicesV2SpecificDataError);
            }

            string jobId;
            try
            {
                jobId = await _mediaServicesV2RestWrapper.CreateJobAsync(
                    jobName,
                    processorId,
                    inputAssetId,
                    preset,
                    outputAssetName,
                    outputAssetStorageAccountName,
                    correlationData: base64UrlEncodedCorrelationDataJsonString,
                    notificationEndPointId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2SubmitMesJobFailure, e, new
                {
                    jobName,
                    processorId,
                    inputAssetId,
                    preset,
                    outputAssetName,
                    outputAssetStorageAccountName,
                    base64UrlEncodedCorrelationDataJsonString,
                    notificationEndPointId
                });
                throw new GridwichEncodeCreateJobException($"Could not start media encoder standard job.", null, e, LogEventIds.MediaServicesV2SubmitMesJobFailure);
            }
            return jobId;
        }


        /// <inheritdoc/>
        public async Task<JObject> GetOperationContextForJobAsync(string jobId)
        {
            IDictionary<string, string> correlationDataDictionary = await GetCorrelationDataDictionaryAsync(jobId).ConfigureAwait(false);
            if (correlationDataDictionary == null || !correlationDataDictionary.ContainsKey("operationContext"))
            {
                _log.LogEventObject(LogEventIds.MediaServicesV2FailedToGetJobOpContext, jobId);
                throw new GridwichEncodeCreateJobException($"Could not get operationContext.", null, null, LogEventIds.MediaServicesV2CorrelationDataError);
            }
            var operationContextJsonString = correlationDataDictionary["operationContext"];
            var operationContext = JObject.Parse(operationContextJsonString);
            return operationContext;
        }

        private async Task<IDictionary<string, string>> GetCorrelationDataDictionaryAsync(string jobId)
        {
            IDictionary<string, string> correlationDataDictionary;
            try
            {
                (_, string firstTaskName) = await _mediaServicesV2RestWrapper.GetFirstTaskAsync(jobId).ConfigureAwait(false);
                var base64UrlEncodedCorrelationDataJsonString = firstTaskName;
                var correlationDataJsonString = Base64UrlEncoder.Decode(base64UrlEncodedCorrelationDataJsonString);
                correlationDataDictionary = JsonConvert.DeserializeObject<IDictionary<string, string>>(correlationDataJsonString);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2CorrelationDataError, e, jobId);
                throw new GridwichEncodeCreateJobException($"Could not get correlationDataDictionary from {jobId}.", null, e, LogEventIds.MediaServicesV2CorrelationDataError);
            }
            return correlationDataDictionary;
        }

        /// <inheritdoc/>
        public async Task<Uri[]> CopyOutputAssetToOutputContainerAsync(string jobId)
        {
            IDictionary<string, string> correlationDataDictionary = await GetCorrelationDataDictionaryAsync(jobId).ConfigureAwait(false);
            if (correlationDataDictionary == null || !correlationDataDictionary.ContainsKey("outputAssetContainer"))
            {
                _log.LogEventObject(LogEventIds.MediaServicesV2CorrelationDataError, jobId);
                throw new GridwichEncodeCreateJobException($"Expected outputAssetContainer in correlationDataDictionary from {jobId}.", null, null, LogEventIds.MediaServicesV2CorrelationDataError);
            }
            var outputAssetContainer = correlationDataDictionary["outputAssetContainer"];
            var outputAssetContainerUri = new Uri(outputAssetContainer);
            (string firstOutputAssetId, _) = await _mediaServicesV2RestWrapper.GetFirstOutputAssetAsync(jobId).ConfigureAwait(false);

            (_, Uri outputAssetUri) = await _mediaServicesV2RestWrapper.GetAssetNameAndUriAsync(firstOutputAssetId).ConfigureAwait(false);

            IEnumerable<string> outputAssetFileNames = await _mediaServicesV2RestWrapper.GetAssetFilesNames(firstOutputAssetId).ConfigureAwait(false);

            List<Uri> outputUris = new List<Uri> { };

            foreach (var assetFileName in outputAssetFileNames)
            {
                try
                {
                    var sourceUriBuilder = new BlobUriBuilder(outputAssetUri)
                    {
                        BlobName = assetFileName
                    };

                    var destUriBuilder = new BlobUriBuilder(outputAssetContainerUri)
                    {
                        BlobName = assetFileName
                    };

                    var s = new Stopwatch();
                    s.Start();
                    var context = StorageClientProviderContext.None;
                    var copyFromUriOperation = await _storageService.BlobCopy(sourceUriBuilder.ToUri(), destUriBuilder.ToUri(), context).ConfigureAwait(false);
                    var response = await copyFromUriOperation.WaitForCompletionAsync().ConfigureAwait(false);
                    s.Stop();
                    outputUris.Add(destUriBuilder.ToUri());
                    DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
                    _log.LogEventObject(LogEventIds.MediaServicesV2CopyFileCompleted, new { CopyElapsedMilliseconds = s.ElapsedMilliseconds.ToString(dtfi) });
                }
                catch (Exception e)
                {
                    _log.LogExceptionObject(LogEventIds.MediaServicesV2CopyOutputAssetError, e, new { assetFileName, jobId });
                    throw new GridwichEncodeCreateJobException($"Could not copy output asset for {jobId}.", null, e, LogEventIds.MediaServicesV2CopyOutputAssetError);
                }
            }

            return outputUris.ToArray();
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAssetsForV2JobAsync(string jobId)
        {
            try
            {
                (string firstInputAssetId, _) = await _mediaServicesV2RestWrapper.GetFirstInputAssetAsync(jobId).ConfigureAwait(false);
                await _mediaServicesV2RestWrapper.DeleteAssetAsync(firstInputAssetId).ConfigureAwait(false);

                (string firstOutputAssetId, _) = await _mediaServicesV2RestWrapper.GetFirstOutputAssetAsync(jobId).ConfigureAwait(false);
                await _mediaServicesV2RestWrapper.DeleteAssetAsync(firstOutputAssetId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2DeleteError, e, jobId);
                throw new GridwichEncodeCreateJobException($"Could not delete asset for {jobId}.", null, e, LogEventIds.MediaServicesV2DeleteError);
            }
            return true;
        }

        private static string GetInputAssetName(BlobUriBuilder sourceUriBuilder)
        {
            return $"V2-{sourceUriBuilder.AccountName}-{sourceUriBuilder.BlobContainerName}-Input";
        }

        private static string GetOutputAssetName(string outputAssetStorageAccountName, BlobUriBuilder outputContainerUriBuilder)
        {
            if (outputContainerUriBuilder.BlobContainerName != outputContainerUriBuilder.BlobContainerName.ToLower(CultureInfo.InvariantCulture))
            {
                throw new ArgumentException($"ContainerName {outputContainerUriBuilder.BlobContainerName} must be lowercase.");
            }
            return $"V2-{outputAssetStorageAccountName}-{outputContainerUriBuilder.BlobContainerName}-Output";
        }

        private static string GenerateJobName(string outputAssetName)
        {
            var provider = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
            string uniqueness = Guid.NewGuid().ToString("N", provider).Substring(0, 11);
            return $"{outputAssetName}-{uniqueness}";
        }
    }
}
