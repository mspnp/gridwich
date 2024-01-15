using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers
{
    /// <summary>
    /// Handles a CreateMetadataEvent for a single file by adding the metadata.
    /// </summary>
    public class CreateMetadataHandler : EventGridHandlerBase<CreateMetadataHandler, RequestBlobMetadataCreateDTO>
    {
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateMetadataHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public CreateMetadataHandler(
            IObjectLogger<CreateMetadataHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  "EAB78688-2784-42AF-BC91-6A0E05D4AF6D",
                  new Dictionary<string, string[]>
                  {
                      { CustomEventTypes.RequestCreateMetadata, AllVersionList }
                  })
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobMetadataCreateDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var context = new StorageClientProviderContext(eventData.OperationContext);

            var deserializedMetadata = eventData.BlobMetadata.ToObject<IDictionary<string, string>>();

            await _storageService.SetBlobMetadataAsync(eventData.BlobUri, deserializedMetadata, context).ConfigureAwait(false);
            Log.LogEventObject(LogEventIds.SuccessEventHandlingInCreateMetadataHandler, eventData.BlobUri);

            return new ResponseBlobMetadataSuccessDTO()
            {
                BlobUri = eventData.BlobUri,
                // TODO: get from storage after we have getmetadata function
                BlobMetadata = eventData.BlobMetadata,
                OperationContext = eventData.OperationContext
            };
        }

        /*
        Use the following to test functions that use EventGridTrigger
        Note the use of an array of json EventGrid events in the body.
        Note the header: aeg-event-type: Notification
        Note the endpoint path and params which include the function name: EventGridAdapter

        curl -X POST \
            'http://localhost:5000/api/EventGrid' \
            -H 'Content-Type: application/json' \
            -H 'aeg-event-type: Notification' \
            -H 'cache-control: no-cache' \
            -d '[{
                "eventType": "request.blob.metadata.create",
                "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                "subject": "/createMetadata/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
                "data": {
                    "blobUri": "https://gridwichinbox00sasb.blob.core.windows.net/testingsupport/bbb_4k_60fps_3sec.mp4",
                    "blobMetadata": {
                        "somekey": "value1",
                        "somekey2": "value2"
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
        */
    }
}