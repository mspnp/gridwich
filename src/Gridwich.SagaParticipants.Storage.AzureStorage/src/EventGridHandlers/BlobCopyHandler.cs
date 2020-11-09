using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers
{
    /// <summary>
    /// Handles a CopyBlobEvent for a single media file request sent from Requestor.
    /// </summary>
    public class BlobCopyHandler : EventGridHandlerBase<BlobCopyHandler, RequestBlobCopyDTO>
    {
        private const string HandlerId = "C5BC453D-58CF-4F16-A2FF-16647F6CBF81";

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestBlobCopy, AllVersionList }
            };

        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobCopyHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public BlobCopyHandler(
            IObjectLogger<BlobCopyHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobCopyDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            // 0. Get Inputs from Data
            _ = eventData.SourceUri ?? throw new ArgumentException("Source uri cannot be null");
            _ = eventData.DestinationUri ?? throw new ArgumentException("Destination uri cannot be null");

            var context = new StorageClientProviderContext(eventData.OperationContext);
            // 1. Do Work
            await StartBlobCopy(eventData, context).ConfigureAwait(false);

            // 2. Interpret Work for Event Crafting
            // TODO: Do we want check Copy Results, and tell Requestor in the "Scheduled" event???
            // bool done = copyResult.HasCompleted;
            // var response = copyResult.UpdateStatus();
            //
            // SE: The sequence needs to stay as Request/Scheduled/Done (RSD).  Confirmed that
            //     Requestor is fine with the fact that they may receive RSD or RDS - latter if
            //     copy completes synchronously.  What might be worth checking is whether Requestor
            //     could benefit from an extra flag in the scheduled response indicating if the copy
            //     is already done.

            var metadata = await _storageService.GetBlobMetadataAsync(eventData.SourceUri, context).ConfigureAwait(false);

            return new ResponseBlobCopyScheduledDTO
            {
                SourceUri = eventData.SourceUri,
                BlobMetadata = metadata,
                DestinationUri = eventData.DestinationUri,
                OperationContext = eventData.OperationContext,
            };
        }

        /// <summary>
        /// Starts the Async Copy of the Blob.
        /// </summary>
        /// <param name="copyInputs">RequestBlobCopyDTO object.</param>
        /// <param name="context">The operation context.</param>
        /// <returns>Response from the Async Blob Copy operation.</returns>
        private async Task<CopyFromUriOperation> StartBlobCopy(RequestBlobCopyDTO copyInputs, StorageClientProviderContext context)
        {
            var isValidSourceUri = Uri.TryCreate(copyInputs.SourceUri.ToString(), UriKind.Absolute, out Uri sourceUri);
            var isValidDestUri = Uri.TryCreate(copyInputs.DestinationUri.ToString(), UriKind.Absolute, out Uri destUri);

            if (!isValidSourceUri)
            {
                throw new GridwichArgumentException(nameof(isValidSourceUri), "Invalid source uri.",
                    LogEventIds.InvalidUriInBlobCopyHandler, context.ClientRequestIdAsJObject);
            }

            if (!isValidDestUri)
            {
                throw new GridwichArgumentException(nameof(isValidDestUri), "Invalid destination uri.",
                    LogEventIds.InvalidUriInBlobCopyHandler, context.ClientRequestIdAsJObject);
            }

            return await _storageService.BlobCopy(sourceUri, destUri, context).ConfigureAwait(false);
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
    "id": "string",
    "topic": "string",
    "subject": "string",
    "eventType": "request.blob.copy",
    "eventTime": "2020-01-24T22:54:08.103Z",
    "metadataVersion": "string",
    "dataVersion": "string",
    "data" : {
        "operationContext" : {"somehappykey" : "somehappyvalue"},
        "sourceUri" : "https://gridwichinbox00sasb.blob.core.windows.net/test00/elephantsDream_hd.mp4",
        "destinationUri" : "https://gridwichlts00sasb.blob.core.windows.net/test00/elephantsDream_hd-copy.mp4",
    }
  }
]'

        */
    }
}