using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.EventGridHandlers
{
    /// <summary>
    /// Handles a MediaServicesLocatorDelete request to unpublish content.
    /// </summary>
    public class MediaServicesLocatorDeleteHandler : EventGridHandlerBase<MediaServicesLocatorDeleteHandler, RequestMediaServicesLocatorDeleteDTO>
    {
        private const string HandlerId = "ff552563-5c1a-445a-ac7d-32fe6b89f865";
        private readonly IMediaServicesV3PublicationService _mediaServicesV3PublicationService;

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestMediaservicesLocatorDelete, AllVersionList }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesLocatorDeleteHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="mediaServicesV3PublicationService">mediaServicesV3PublicationService.</param>
        public MediaServicesLocatorDeleteHandler(
            IObjectLogger<MediaServicesLocatorDeleteHandler> log,
            IEventGridPublisher eventPublisher,
            IMediaServicesV3PublicationService mediaServicesV3PublicationService)
            : base(
                  log,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _mediaServicesV3PublicationService = mediaServicesV3PublicationService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestMediaServicesLocatorDeleteDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            // Call the service that will do work:
            ServiceOperationResultMediaServicesV3LocatorDelete locatorCreateResults = await _mediaServicesV3PublicationService.LocatorDeleteAsync(
                eventData.LocatorName,
                eventData.OperationContext).ConfigureAwait(false);

            // Convert the service operation result into a success DTO:
            return new ResponseMediaServicesLocatorDeleteSuccessDTO
            {
                LocatorName = locatorCreateResults.LocatorName,
                OperationContext = locatorCreateResults.OperationContext
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
                        "eventType": "request.mediaservices.locator.delete",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "locatorName": "locator-5c2e12323a4",
                            "operationContext": {
                                "test": "test1",
                                "someId": 1000
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