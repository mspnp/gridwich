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
    /// Handles a ContainerAccessChangeHandler event which makes a container publicly accessible (or not).
    /// </summary>
    public class ContainerAccessChangeHandler : EventGridHandlerBase<ContainerAccessChangeHandler, RequestContainerAccessChangeDTO>
    {
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerAccessChangeHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public ContainerAccessChangeHandler(
            IObjectLogger<ContainerAccessChangeHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  "CAFEBABE-D7C9-4C66-B21A-4C775030BE09",
                  new Dictionary<string, string[]>
                  {
                      { CustomEventTypes.RequestBlobContainerAccessChange, AllVersionList }
                  })
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestContainerAccessChangeDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));
            var context = new StorageClientProviderContext(eventData.OperationContext);

            await _storageService.ContainerSetPublicAccessAsync(
                eventData.StorageAccountName,
                eventData.ContainerName,
                eventData.AccessType,
                context).ConfigureAwait(false);

            return new ResponseContainerAccessChangeSuccessDTO
            {
                StorageAccountName = eventData.StorageAccountName,
                ContainerName = eventData.ContainerName,
                AccessType = eventData.AccessType,
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
                        "eventType": "request.blob.container.access.change",
                        "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/containerAccessChangeSuccessResponse/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
                        "data": {
                            "storageAccountName": "gridwichinbox00sasb",
                            "containerName": "testcontainersupport",
                            "accessType": "None",
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