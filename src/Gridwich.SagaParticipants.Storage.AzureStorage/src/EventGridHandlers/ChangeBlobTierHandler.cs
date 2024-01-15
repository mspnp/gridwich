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
    /// Handles a ChangeBlobTierEvent, moving a blob from one specific storage tier to another.
    /// </summary>
    public class ChangeBlobTierHandler : EventGridHandlerBase<ChangeBlobTierHandler, RequestBlobTierChangeDTO>
    {
        private const string HandlerId = "673572f4-1e57-4b32-a7e6-57ae433eb9c1";

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestBlobTierChange, AllVersionList }
            };

        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeBlobTierHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="storageService">storageService.</param>
        public ChangeBlobTierHandler(
            IObjectLogger<ChangeBlobTierHandler> log,
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
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobTierChangeDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var context = new StorageClientProviderContext(eventData.OperationContext);
            var rehydratePriority = eventData.RehydratePriority;

            await _storageService.ChangeBlobTierAsync(new Uri(eventData.BlobUri), eventData.AccessTier, rehydratePriority, context).ConfigureAwait(false);

            return new ResponseBlobTierChangeSuccessDTO()
            {
                AccessTier = eventData.AccessTier,
                BlobUri = eventData.BlobUri,
                OperationContext = eventData.OperationContext,
                RehydratePriority = eventData.RehydratePriority
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
                        "eventType": "request.blob.tier.change",
                        "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/azfnmediainfo/providers/Microsoft.EventGrid/topics/DeliveryAvailableDev1",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/createMetadata/fad8e1ca-daba-4d91-a45a-b9fd3dc47004",
                        "data": {
                            "blobUri": "https://gridwichinbox00sasb.blob.core.windows.net/testingsupport/bbb_4k_60fps_3sec.mp4",
                            "accessTier": "Cool",
                            "rehydratePriority": "Standard",
                            "operationContext": {
                                "test": "test"
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