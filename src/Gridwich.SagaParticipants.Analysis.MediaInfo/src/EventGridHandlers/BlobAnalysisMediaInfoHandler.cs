using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.EventGridHandlers
{
    /// <summary>
    /// An IEventGridHandler that handles a BlobAnalysisCreate request, analyzing the file and publishing the report to event grid.
    /// </summary>
    public class BlobAnalysisMediaInfoHandler : EventGridHandlerBase<BlobAnalysisMediaInfoHandler, RequestBlobAnalysisCreateDTO>
    {
        private const string HandlerId = "A7250940-98C8-4CC5-A66C-45A2972FF3A2";

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestBlobAnalysisCreate, AllVersionList }
            };

        private readonly IStorageService _storageService;
        private readonly IMediaInfoReportService _mediaInfoReportService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobAnalysisMediaInfoHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">event publisher.</param>
        /// <param name="mediaInfoReportService">media info report provider.</param>
        public BlobAnalysisMediaInfoHandler(
            IObjectLogger<BlobAnalysisMediaInfoHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher,
            IMediaInfoReportService mediaInfoReportService)
            : base(
                  logger,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _storageService = storageService;
            _mediaInfoReportService = mediaInfoReportService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobAnalysisCreateDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));
            _ = eventData.BlobUri ?? throw new ArgumentException("Blob uri cannot be null.");

            Log.LogEventObject(LogEventIds.AboutToCallAnalysisDeliveryEntry, eventData);
            var context = new StorageClientProviderContext(eventData.OperationContext);

            var analysisResult = await _mediaInfoReportService.GetMediaInfoCompleteInformForUriAsync(eventData.BlobUri, context).ConfigureAwait(true);
            Log.LogEventObject(LogEventIds.AnalysisOfDeliveryFileSuccessful, eventData);

            JObject blobMetadata = await _storageService.GetBlobMetadataAsync(eventData.BlobUri, context).ConfigureAwait(false);

            return new ResponseBlobAnalysisSuccessDTO
            {
                OperationContext = eventData.OperationContext,
                BlobUri = eventData.BlobUri,
                BlobMetadata = blobMetadata,
                AnalysisResult = analysisResult,
            };
        }

        /*
        Use the following to test functions that use EventGridTrigger
        Note the use of an array of json EventGrid events in the body.
        Note the header: aeg-event-type: Notification
        Note the endpoint path and params which include the function name: EventGridAdapter

curl -X POST \
'http://localhost:7071/api/EventGrid' \
-H 'Content-Type: application/json' \
-H 'aeg-event-type: Notification' \
-H 'cache-control: no-cache' \
-d '[
  {
    "eventType": "request.blob.analysis.create",
    "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
    "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
    "subject": "/requestBlobAnalysisCreate/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
    "data": {
      "blobUri": "https://hgridwichinbox00sasb.blob.core.windows.net/test00/BBB_trailer_0325T1031.mp4",
      "md5": "1000",
      "operationContext": {"foo":"bar", "one":"two"},
      "analyzerSpecificData": {
        "mediaInfo": {
          "commandLineOptions": {
            "Complete": "1",
            "Output": "JSON"
          }
        }
      }
    },
    "eventTime": "2019-12-06T17:23:51.4701403-05:00",
    "metadataVersion": null,
    "dataVersion": "1.0"
  }
]'
        */
    }
}