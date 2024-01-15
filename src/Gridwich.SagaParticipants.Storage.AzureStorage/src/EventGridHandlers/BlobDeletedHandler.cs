using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers
{
    /// <summary>
    /// Handles a StorageBlobDeletedEvent for a single media file representing the full delivery for a timeline.
    /// </summary>
    public class BlobDeletedHandler : EventGridHandlerBase<BlobDeletedHandler, StorageBlobDeletedEventData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDeletedHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public BlobDeletedHandler(
            IObjectLogger<BlobDeletedHandler> logger,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  "ed59019b-5e8e-43a7-8b83-490611cdb722",
                  new Dictionary<string, string[]>
                  {
                      { EventTypes.StorageBlobDeletedEvent, AllVersionList }
                  })
        { }

        /// <inheritdoc/>
        protected override Task<ResponseBaseDTO> DoWorkAsync(StorageBlobDeletedEventData eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var context = StorageClientProviderContext.CreateSafe(eventData.ClientRequestId);

            if (!Uri.TryCreate(eventData.Url, UriKind.Absolute, out Uri outputUri))
            {
                throw new GridwichArgumentException(nameof(eventData.Url), $"Invalid uri {eventData.Url}",
                    LogEventIds.FailedToCreateBlobDeletedDataWithEventDataInBlobDeletedHandler, context.ClientRequestIdAsJObject);
            }

            return Task.FromResult<ResponseBaseDTO>(new ResponseBlobDeleteSuccessDTO
            {
                BlobUri = outputUri,
                OperationContext = context.ClientRequestIdAsJObject,
                DoNotPublish = context.IsMuted,
            });
        }

        /// <inheritdoc/>
        protected override JObject ParseOperationContext(StorageBlobDeletedEventData eventData)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var ctx = new StorageClientProviderContext(eventData.ClientRequestId);
            return ctx.ClientRequestIdAsJObject;
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
          -d '[{
                "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/Storage/providers/Microsoft.Storage/storageAccounts/gridwichinbox01sasb",
                "subject": "/blobServices/default/containers/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4",
                "eventType": "Microsoft.Storage.BlobDeleted",
                "eventTime": "2017-06-26T18:41:00.9584103Z",
                "id": "831e1650-001e-001b-66ab-eeb76e069631",
                "data": {
                  "api": "DeleteBlob",
                  "clientRequestId": "6d79dbfb-0e37-4fc4-981f-442c9ca65760",
                  "requestId": "831e1650-001e-001b-66ab-eeb76e000000",
                  "eTag": "0x8D4BCC2E4835CD0",
                  "contentType": "text/plain",
                  "contentLength": 8042577,
                  "blobType": "BlockBlob",
                  "url": "https://gridwichinbox00sasb.blob.core.windows.net/test00/BBB_trailer_0325T1031.mp4",
                  "sequencer": "00000000000004420000000000028963",
                  "storageDiagnostics": {
                    "batchId": "b68529f3-68cd-4744-baa4-3c0498ec19f0"
                  }
                },
                "dataVersion": "1.0",
                "metadataVersion": "1"
              }]'

        */
    }
}