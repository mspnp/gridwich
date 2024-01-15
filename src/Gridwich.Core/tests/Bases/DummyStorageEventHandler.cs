using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.CoreTests.Bases
{
    /// <inheritdoc />
    public class DummyStorageEventHandler : EventGridHandlerBase<DummyStorageEventHandler, RequestBlobMetadataCreateDTO>
    {
        private readonly IStorageService _storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DummyStorageEventHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger for this class.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="eventPublisher">The event publisher service.</param>
        public DummyStorageEventHandler(
            IObjectLogger<DummyStorageEventHandler> logger,
            IStorageService storageService,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  "654BCC8B-B61C-4764-B536-541AF3779818",
                  new Dictionary<string, string[]>
                    {
                        { CustomEventTypes.RequestBlobCopy, AllVersionList },
                        { "Not External Request Event", AllVersionList }
                    })
        {
            _storageService = storageService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestBlobMetadataCreateDTO eventData, string eventType)
        {
            var metadata = await _storageService.GetBlobMetadataAsync(new Uri("https://example.com"), StorageClientProviderContext.None).ConfigureAwait(false);
            return new ResponseBlobMetadataSuccessDTO
            {
                BlobMetadata = metadata,
                BlobUri = new Uri("https://example.com")
            };
        }
    }
}
