using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;

using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers
{
    /// <summary>
    /// Handles a BlobDelete a single blob.
    /// </summary>
    public class BlobDeleteHandler : EventGridHandlerBase<BlobDeleteHandler, RequestBlobDeleteDTO>
    {
        // Housekeeping Information for this Handler
        private const string UniqueIdForHandler = "13244CB5-64F7-4C64-87B7-D7B0001E028F";
        private static readonly Dictionary<string, string[]> EventTypesHandled
            = new Dictionary<string, string[]>
                {
                    { CustomEventTypes.RequestBlobDelete, AllVersionList }
                };

        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDeleteHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public BlobDeleteHandler(
            IObjectLogger<BlobDeleteHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(logger, eventPublisher, UniqueIdForHandler, EventTypesHandled)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Creates the Data payload of an EventGrid event by transforming a CreateMetadata event.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="eventType">The event type.</param>
        /// <returns>AddBlobMetadataAsync to be used in an EventGrid message of the same type.</returns>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobDeleteDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            // 0.2 ensure the URI is rewritten in absolute URI format.
            if (!Uri.TryCreate(eventData.BlobUri.ToString(), UriKind.Absolute, out Uri blobUri))
            {
                throw new GridwichArgumentException(nameof(eventData.BlobUri), "Invalid uri supplied to blob delete operation.",
                    LogEventIds.InvalidUriInBlobDeleteHandler, eventData.OperationContext);
            }

            var requestContext = new StorageClientProviderContext(eventData.OperationContext, trackETag: true);

            ////////////////////////////////////////////
            // 1. Do Work

            // 1.1 Get the Blob Metadata needed for the response.
            JObject metadata = await _storageService.GetBlobMetadataAsync(blobUri, requestContext).ConfigureAwait(false) ?? new JObject();

            // 1.2 Request the actual deletion from Storage
            var isFoundAndDeleted = await _storageService.BlobDelete(blobUri, requestContext).ConfigureAwait(false);

            if (isFoundAndDeleted)
            {
                // 2. Interpret Work for Event Crafting
                return new ResponseBlobDeleteScheduledDTO
                {
                    BlobUri = eventData.BlobUri,
                    OperationContext = eventData.OperationContext,
                    BlobMetadata = metadata,
                };
            }

            // Special case -- if the blob wasn't there to be able to delete, send a failure back to Requestor
            Log.LogEventObject(out Uri uriLocator, LogEventIds.NoSuchBlobInBlobDeleteHandler, metadata);
            return GetGridwichFailureDTO(LogEventIds.NoSuchBlobInBlobDeleteHandler.Name, eventData.OperationContext, LogEventIds.NoSuchBlobInBlobDeleteHandler.Id, uriLocator);
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
                "eventType": "request.blob.delete",
                "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                "subject": "/deleteBlob/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
                "data": {
                    "blobUri": "https://aaaafoobar22111.blob.core.windows.net/gridwich/lock.json",
                    "operationContext":  {
                        "anotherkey": "value3",
                        "anotherkey2": "value4"
                    }
                    },
                "eventTime": "2020-02-26T17:23:51.4701403-05:00",
                "metadataVersion": null,
                "dataVersion": "1.0"
                }]'
        */
    }
}