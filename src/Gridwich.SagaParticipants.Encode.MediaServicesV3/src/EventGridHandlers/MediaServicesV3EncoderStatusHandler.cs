using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.EventGridHandlers
{
    /// <summary>
    /// Handles a Media Services Encoder status messages.
    /// This class will handle N types of events from Media Services Encoder
    /// </summary>
    public class MediaServicesV3EncoderStatusHandler : EventGridHandlerBase<MediaServicesV3EncoderStatusHandler, object>
    {
        private const string ProcessingStatus = "Processing";

        private const string HandlerId = "2229453D-58CF-4F16-A2FF-19647F6CBF81";
        // The eventsToHandle Dictionary is an object that binds inbound eventTypes from the Flip notification service
        // to the proper data structure classes.  These data classes (models) define the schema of the inbound Json payloads,
        // and have associated code that knows how to convert themselves into the expected format for Gridwich encoder status messages.

        private static readonly Dictionary<string, Type> EventsToHandle = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase)
        {
            // We support this 5 AMS v3 events.
             { EventTypes.MediaJobCanceledEvent, typeof(MediaJobCanceledEventData) },
             { EventTypes.MediaJobErroredEvent, typeof(MediaJobErroredEventData) },
             { EventTypes.MediaJobFinishedEvent, typeof(MediaJobFinishedEventData) },
             { EventTypes.MediaJobOutputProgressEvent, typeof(MediaJobOutputProgressEventData) },
             { EventTypes.MediaJobScheduledEvent, typeof(MediaJobScheduledEventData) },
        };
        private static readonly Dictionary<string, string[]> AcceptedEvents = EventsToHandle.ToDictionary(i => i.Key, j => AllVersionList);
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3EncoderStatusHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="storageService">storageService.</param>
        public MediaServicesV3EncoderStatusHandler(
            IObjectLogger<MediaServicesV3EncoderStatusHandler> logger,
            IEventGridPublisher eventPublisher,
            IStorageService storageService)
            : base(
                  logger,
                  eventPublisher,
                  HandlerId, AcceptedEvents)
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(object eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            switch (eventType)
            {
                case EventTypes.MediaJobCanceledEvent:
                {
                    var canceledEventData = (MediaJobCanceledEventData)eventData;
                    this.Log.LogEventObject(out Uri uriLocator, LogEventIds.MediaServicesV3JobCanceledReceived, canceledEventData);
                    return GetGridwichFailureDTO(LogEventIds.MediaServicesV3JobCanceledReceived.Name,
                        GetOperationContext(canceledEventData), LogEventIds.MediaServicesV3JobCanceledReceived.Id, uriLocator);
                }

                case EventTypes.MediaJobErroredEvent:
                {
                    var errorData = (MediaJobErroredEventData)eventData;
                    var errorMessage = BuildErrorMessage(errorData);

                    // - Raise Event "Job Errored"
                    this.Log.LogEventObject(out Uri uriLocator, LogEventIds.MediaServicesV3JobErroredReceived, errorData);
                    return GetGridwichFailureDTO(
                        $"{LogEventIds.MediaServicesV3JobErroredReceived.Name}: {errorMessage}",
                        GetOperationContext(errorData),
                        LogEventIds.MediaServicesV3JobErroredReceived.Id,
                        uriLocator);
                }

                case EventTypes.MediaJobFinishedEvent:
                {
                    var finishedData = (MediaJobFinishedEventData)eventData;
                    var operationContext = GetOperationContext(finishedData);
                    var encodeStatus = new ResponseEncodeSuccessDTO(CustomEventTypes.ResponseEncodeMediaServicesV3Success)
                    {
                        OperationContext = operationContext
                    };
                    // Get the Blob Uri from Azure Media Services in the correlationData
                    finishedData.CorrelationData.TryGetValue("outputAssetContainer", out var uri);
                    encodeStatus.Outputs = await GetOutputBlobs(uri, operationContext).ConfigureAwait(false);
                    return encodeStatus;
                }

                case EventTypes.MediaJobOutputProgressEvent:
                {
                    var progressData = (MediaJobOutputProgressEventData)eventData;

                    var encodeStatus = new ResponseEncodeProcessingDTO(CustomEventTypes.ResponseEncodeMediaServicesV3Processing);
                    if (progressData.Progress != null)
                    {
                        encodeStatus.PercentComplete = (int)progressData.Progress;
                    }

                    // CurrentStatus = "Processing",....
                    encodeStatus.CurrentStatus = ProcessingStatus;
                    encodeStatus.OperationContext = GetOperationContext(progressData);
                    return encodeStatus;
                }

                case EventTypes.MediaJobScheduledEvent:
                {
                    var scheduledData = (MediaJobScheduledEventData)eventData;

                    var encodeStatus = new ResponseEncodeSuccessDTO(CustomEventTypes.ResponseEncodeMediaServicesV3Scheduled)
                    {
                        OperationContext = GetOperationContext(scheduledData)
                    };
                    return encodeStatus;
                }

                default:
                    throw new GridwichMediaServicesV3NotHandledException($"The provided event type {eventType} cannot be handled by this handler. ", null);
            }
        }

        /// <summary>
        /// Reads the .Data payload of an EventGrid event for a Media Services event.
        /// </summary>
        /// <param name="eventGridEvent">RequestBlobCopy event.</param>
        /// <returns>Data object to be used in from the EventGrid message.</returns>
        protected override object GetEventData(EventGridEvent eventGridEvent)
        {
            _ = eventGridEvent ?? throw new ArgumentNullException(nameof(eventGridEvent));

            if (EventsToHandle.TryGetValue(eventGridEvent.EventType, out var eventClass))
            {
                return JsonConvert.DeserializeObject(eventGridEvent.Data.ToString(), eventClass);
            }

            // TODO: Fix storage context
            throw new GridwichMediaServicesV3NotHandledException($"The provided event type {eventGridEvent.EventType} cannot be deserialized by this handler. ", null);
        }

        /// <summary>
        /// Get the operation context for MediaJobStateChangeEventData data.
        /// </summary>
        /// <param name="data">The MediaJobStateChangeEventData data (or derived classes).</param>
        /// <returns>The operationContext.</returns>
        private static JObject GetOperationContext(MediaJobStateChangeEventData data)
        {
            _ = data ?? throw new ArgumentNullException(nameof(data));

            if (data.CorrelationData.TryGetValue("operationContext", out var opContext))
            {
                return JsonHelpers.JsonToJObject(opContext);
            }

            throw new GridwichMediaServicesV3NotHandledException($"Unable to get operation context for event {data}", null);
        }

        /// <summary>
        /// Get the operation context for MediaJobOutputProgressEventData data.
        /// </summary>
        /// <param name="data">The MediaJobOutputProgressEventData data.</param>
        /// <returns>The operationContext.</returns>
        private static JObject GetOperationContext(MediaJobOutputProgressEventData data)
        {
            _ = data ?? throw new ArgumentNullException(nameof(data));

            if (data.JobCorrelationData.TryGetValue("operationContext", out var opContext))
            {
                return JsonHelpers.JsonToJObject(opContext);
            }

            throw new GridwichMediaServicesV3NotHandledException($"Unable to get operation context for event {data}", null);
        }

        private async Task<Output[]> GetOutputBlobs(string uriString, JObject operationContext)
        {
            var containerUri = new Uri(uriString);
            var blobs = new List<BlobItem>(await _storageService.ListBlobsAsync(containerUri, new StorageClientProviderContext(operationContext)).ConfigureAwait(false));
            return blobs.Select(blob => new Output()
            {
                BlobUri = new BlobUriBuilder(containerUri) { BlobName = blob.Name }.ToString(),
            }).ToArray();
        }

        /// <summary>
        /// Utility function thar returns a list of errors and messages for a AMS v3 job error.
        /// </summary>
        /// <param name="errorData">Error status.</param>
        /// <returns>Error message.</returns>
        private static string BuildErrorMessage(MediaJobErroredEventData errorData)
        {
            var messageBuilder = new StringBuilder();
            if ((errorData != null) && (errorData.Outputs != null) && (errorData.Outputs.Count > 0))
            {
                // Collect the errors for all the outputs
                var indexO = 0;
                foreach (var output in errorData.Outputs.OrEmptyIfNull())
                {
                    messageBuilder.AppendLine($"Output-{indexO}: {output.Error?.Message}");
                    if (output.Error != null)
                    {
                        var indexD = 0;
                        foreach (var detail in output.Error.Details.OrEmptyIfNull())
                        {
                            messageBuilder.AppendLine($"Output-{indexO} Detail-{indexD}: {detail.Message}");
                            indexD++;
                        }
                    }
                    indexO++;
                }
            }

            return messageBuilder.ToString();
        }

        /*
            Use the following to test functions that use EventGridTrigger
            Note the use of an array of json EventGrid events in the body.
            Note the header: aeg-event-type: Notification
            Note the endpoint path and params which include the function name: EventGridAdapter

    curl -X POST 'http://localhost:7071/api/EventGrid' -H 'Content-Type: application/json' -H 'aeg-event-type: Notification' -H 'cache-control: no-cache' -d '[{
                "eventType": "request.encode.create",
                "topic": "/subscriptions/022c7730-ffe0-4530-9e57-7c5c5067c78c/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                "subject": "/encodeCreate/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
                "data": {
                    "inputs": [ {
                        "bloburi" : "https://gridwichinbox00saprm1.blob.core.windows.net/input05/BBB_trailer_20200225.mp4"
                    }],
                    "outputContainer" : "https://gridwichinbox00saprm1.blob.core.windows.net/output14/",
                    "correlationId":"deprecated",
                    "encoderSpecificData": {
                        "mediaServicesV3": {
                            "transformName":"EncoderNamedPresetAdaptiveStreaming"
                        }
                    },
                    "operationContext":  {
                        "anotherkey": "value3",
                        "anotherkey2": "value4"
                    }
                },
                "eventTime": "2019-12-06T17:23:51.4701403-05:00",
                "metadataVersion": null,
                "dataVersion": "1.0"
                }]'

    // Microsoft.Media.JobStateChange

    curl -X POST http://localhost:7071/api/EventGrid  -H "Content-Type: application/json" -H "aeg-event-type: Notification"  -H "cache-control: no-cache"    -d '[{
        "topic": "/subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Media/mediaservices/<account-name>",
        "subject": "transforms/VideoAnalyzerTransform/jobs/<job-id>",
        "eventType": "Microsoft.Media.JobStateChange",
        "eventTime": "2018-04-20T21:26:13.8978772",
        "id": "b9d38923-9210-4c2b-958f-0054467d4dd7",
        "data":
    {
    "previousState": "Processing",
    "state": "Finished"
    },
    "dataVersion": "1.0",
    "metadataVersion": "1"
    }]'

    // Job processing
    curl -X POST http://localhost:7071/api/EventGrid  -H "Content-Type: application/json" -H "aeg-event-type: Notification"  -H "cache-control: no-cache"    -d '[{
    "topic": "/subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Media/mediaservices/<account-name>",
    "subject": "transforms/VideoAnalyzerTransform/jobs/<job-id>",
    "eventType": "Microsoft.Media.JobProcessing",
    "eventTime": "2018-10-12T16:12:18.0839935",
    "id": "a0a6efc8-f647-4fc2-be73-861fa25ba2db",
    "data": {
    "previousState": "Scheduled",
    "state": "Processing",
    "correlationData": {
                "mediaServicesV3EncoderSpecificData": "" ,
                "inputAssetName": "myinputasset" ,
                "outputAssetContainer": "https://test.com/path/myassetcontainer/" ,
                "outputAssetName": "https://test.com/path/myassetcontainer/" ,
                "operationContext": "MyContext" ,
    }
    },
    "dataVersion": "1.0",
    "metadataVersion": "1"
    }]'


    curl -X POST http://localhost:7071/api/EventGrid  -H "Content-Type: application/json" -H "aeg-event-type: Notification"  -H "cache-control: no-cache"    -d '[{
    "topic": "/subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Media/mediaservices/<account-name>",
    "subject": "transforms/VideoAnalyzerTransform/jobs/<job-id>",
    "eventType": "Microsoft.Media.JobFinished",
    "eventTime": "2018-10-12T16:25:56.4115495",
    "id": "9e07e83a-dd6e-466b-a62f-27521b216f2a",
    "data": {
    "outputs": [
      {
        "@odata.type": "#Microsoft.Media.JobOutputAsset",
        "assetName": "output-7640689F",
        "error": null,
        "label": "VideoAnalyzerPreset_0",
        "progress": 100,
        "state": "Finished"
      }
    ],
    "previousState": "Processing",
    "state": "Finished",
    "correlationData": {
                "mediaServicesV3EncoderSpecificData": "" ,
                "inputAssetName": "myinputasset" ,
                "outputAssetContainer": "https://test.com/path/myassetcontainer/" ,
                "outputAssetName": "https://test.com/path/myassetcontainer/" ,
                "operationContext": "MyContext" ,
    }
    },
    "dataVersion": "1.0",
    "metadataVersion": "1"
    }]'

    // Output progress:

    curl -X POST http://localhost:7071/api/EventGrid  -H "Content-Type: application/json" -H "aeg-event-type: Notification"  -H "cache-control: no-cache"    -d '[{
    "topic": "/subscriptions/<subscription-id>/resourceGroups/belohGroup/providers/Microsoft.Media/mediaservices/<account-name>",
    "subject": "transforms/VideoAnalyzerTransform/jobs/job-5AB6DE32",
    "eventType": "Microsoft.Media.JobOutputProgress",
    "eventTime": "2018-12-10T18:20:12.1514867",
    "id": "00000000-0000-0000-0000-000000000000",
    "data": {
    "jobCorrelationData": {
                "mediaServicesV3EncoderSpecificData": "" ,
                "inputAssetName": "myinputasset" ,
                "outputAssetContainer": "https://test.com/path/myassetcontainer/" ,
                "outputAssetName": "https://test.com/path/myassetcontainer/" ,
                "operationContext": "MyContext" ,
    },
    "label": "VideoAnalyzerPreset_0",
    "progress": 86
    },
    "dataVersion": "1.0",
    "metadataVersion": "1"
    }]'



        */
    }
}