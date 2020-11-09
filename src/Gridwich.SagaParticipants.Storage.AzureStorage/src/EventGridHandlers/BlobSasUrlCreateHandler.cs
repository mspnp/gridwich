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
    /// Handles a BlobSasUrlCreateHandler for a single source URI with a number of seconds to live.
    /// </summary>
    public class BlobSasUrlCreateHandler : EventGridHandlerBase<BlobSasUrlCreateHandler, RequestBlobSasUrlCreateDTO>
    {
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobSasUrlCreateHandler" /> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public BlobSasUrlCreateHandler(
            IObjectLogger<BlobSasUrlCreateHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(logger,
                  eventPublisher,
                  "dfe336ce-4d21-11ea-bb6e-cfd5a68b8278",
                  new Dictionary<string, string[]>
                  {
                      { CustomEventTypes.RequestBlobSasUrlCreate, AllVersionList }
                  })
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobSasUrlCreateDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            // Read SecToLive as a time span.
            var ttl = TimeSpan.FromSeconds(eventData.SecToLive);
            var context = new StorageClientProviderContext(eventData.OperationContext);
            var sasUrl = _storageService.GetSasUrlForBlob(eventData.BlobUri, ttl, context);

            return await Task.FromResult<ResponseBaseDTO>(new ResponseBlobSasUrlSuccessDTO
            {
                SasUrl = new Uri(sasUrl),
                OperationContext = eventData.OperationContext,
            }).ConfigureAwait(false);
        }

        /*
            Use the following to test functions that use EventGridTrigger
            Note the use of an array of json EventGrid events in the body.
            Note the header: aeg-event-type: Notification
            Note the endpoint path and params which include the function name: EventGridAdapter

        curl -k -X POST \
          'https://localhost:7071/EventGrid' \
          -H 'Content-Type: application/json' \
          -H 'aeg-event-type: Notification' \
          -H 'cache-control: no-cache' \
          -d '[{
                "topic": "/subscriptions/9f363fa5-298e-4135-9439-400c0a126fd0/resourceGroups/Storage/providers/Microsoft.Storage/storageAccounts/gridwichinbox01sasb",
                "subject": "/blobServices/default/containers/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4",
                "eventType": "request.blob.sas-url.create",
                "eventTime": "2017-06-26T18:41:00.9584103Z",
                "id": "831e1650-001e-001b-66ab-eeb76e069631",
                "data": {
                  "sourceUri": "/sources/2020/02/11/bbb.mp4",
                  "secToLive": "600",
                  "operationalContext": {}
                },
                "dataVersion": "1.0",
                "metadataVersion": "1"
              }]'

        */
    }
}
