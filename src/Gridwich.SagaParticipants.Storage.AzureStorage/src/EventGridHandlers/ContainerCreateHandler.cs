using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers
{
    /// <summary>
    /// Handles a ContainerCreateEvent, it creates a new container for an storageAccount that already exists.
    /// </summary>
    public class ContainerCreateHandler : EventGridHandlerBase<ContainerCreateHandler, RequestContainerCreateDTO>
    {
        private const string HandlerId = "24b0230e-b00f-7fae-e505-8ea18fd0b4e8";

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestBlobContainerCreate, AllVersionList }
            };

        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerCreateHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="storageService">storageService.</param>
        public ContainerCreateHandler(
            IObjectLogger<ContainerCreateHandler> log,
            IEventGridPublisher eventPublisher,
            IStorageService storageService)
            : base(
                  log,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestContainerCreateDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var context = new StorageClientProviderContext(eventData.OperationContext);

            await _storageService.ContainerCreateAsync(eventData.StorageAccountName, eventData.ContainerName, context).ConfigureAwait(false);

            return new ResponseContainerCreatedSuccessDTO()
            {
                StorageAccountName = eventData.StorageAccountName,
                ContainerName = eventData.ContainerName,
                OperationContext = eventData.OperationContext
            };
        }

        /*
            curl -X POST \
              'http://localhost:7071/api/EventGrid' \
              -H 'Content-Type: application/json' \
              -H 'aeg-event-type: Notification' \
              -H 'cache-control: no-cache' \
              -d '[
                    {
                        "eventType": "request.blob.container.create",
                        "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/createMetadata/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
                        "data": {
                            "storageAccountName": "gridwichinbox00sasb",
                            "containerName": "testcontainersupport",
                            "operationContext": {
                                "test1": "test2"
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
