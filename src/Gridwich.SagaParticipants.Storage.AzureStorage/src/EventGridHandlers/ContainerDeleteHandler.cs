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
    /// Handles a ContainerDeleteEvent, deletes an existing container inside a storageAccount.
    /// </summary>
    public class ContainerDeleteHandler : EventGridHandlerBase<ContainerDeleteHandler, RequestContainerDeleteDTO>
    {
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerDeleteHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public ContainerDeleteHandler(
            IObjectLogger<ContainerDeleteHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  "B8AC2788-7818-11EA-BC55-024DAC130003",
                  new Dictionary<string, string[]>
                  {
                      { CustomEventTypes.RequestBlobContainerDelete, AllVersionList }
                  })
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestContainerDeleteDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var context = new StorageClientProviderContext(eventData.OperationContext);

            await _storageService.ContainerDeleteAsync(eventData.StorageAccountName, eventData.ContainerName, context).ConfigureAwait(false);

            return new ResponseContainerDeleteSuccessDTO
            {
                StorageAccountName = eventData.StorageAccountName,
                ContainerName = eventData.ContainerName
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
                        "eventType": "request.blob.container.delete",
                        "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/blobContainerDelete/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
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