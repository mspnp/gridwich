using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;

using Microsoft.Azure.EventGrid.Models;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.EventGridHandlers
{
    /// <summary>
    /// Handles a MediaServicesLocatorCreate request to publish content.
    /// </summary>
    public class MediaServicesLocatorCreateHandler : EventGridHandlerBase<MediaServicesLocatorCreateHandler, RequestMediaServicesLocatorCreateDTO>
    {
        private const string HandlerId = "1CC572f4-1e57-4b32-a7e6-57ae433eb9c1";
        private readonly IMediaServicesV3PublicationService _mediaServicesV3PublicationService;

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestMediaservicesLocatorCreate, AllVersionList }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesLocatorCreateHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="mediaServicesV3PublicationService">mediaServicesV3PublicationService.</param>
        public MediaServicesLocatorCreateHandler(
            IObjectLogger<MediaServicesLocatorCreateHandler> log,
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

        /// <summary>
        /// Call the handler implementation directly.  This should only be called by tests.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="eventType">The event type.</param>
        /// <returns>An <see cref="EventGridEvent"/></returns>
        internal Task<ResponseBaseDTO> TestDoWorkAsync(RequestMediaServicesLocatorCreateDTO eventData, string eventType) => DoWorkAsync(eventData, eventType);


        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestMediaServicesLocatorCreateDTO eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            // Call the service that will do work:
            ServiceOperationResultMediaServicesV3LocatorCreate locatorCreateResults = await _mediaServicesV3PublicationService.LocatorCreateAsync(
                eventData.ContainerUri,
                eventData.StreamingPolicyName,
                eventData.ContentKeyPolicyName,
                eventData.TimeBasedFilter,
                eventData.OperationContext,
                eventData.GenerateAudioFilters).ConfigureAwait(false);

            // Convert the service operation result into a success DTO:
            return new ResponseMediaServicesLocatorCreateSuccessDTO
            {
                CENCKeyId = locatorCreateResults.CENCKeyId,
                CBCSKeyId = locatorCreateResults.CBCSKeyId,
                DashUri = locatorCreateResults.DashUri,
                HlsUri = locatorCreateResults.HlsUri,
                LocatorName = locatorCreateResults.LocatorName,
                OperationContext = locatorCreateResults.OperationContext,
            };
        }

        /*
         * CLEAR STREAMING

            curl -X POST \
              'http://localhost:7071/api/EventGrid' \
              -H 'Content-Type: application/json' \
              -H 'aeg-event-type: Notification' \
              -H 'cache-control: no-cache' \
              -d '[
                    {
                        "eventType": "request.mediaservices.locator.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "containerUri": "https://gridwichproxy00sasb.blob.core.windows.net/test00",
                            "streamingPolicyName" : "clearStreamingOnly",
                            "contentKeyPolicyName" : null,
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



        DRMCENC Locator Creation (2 DRMs)

          curl -X POST \
              'http://localhost:7071/api/EventGrid' \
              -H 'Content-Type: application/json' \
              -H 'aeg-event-type: Notification' \
              -H 'cache-control: no-cache' \
              -d '[
                    {
                        "eventType": "request.mediaservices.locator.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "containerUri": "https://gridwichproxy00sasb.blob.core.windows.net/fd7b4d3a-8f20-4744-b7a0-c26252580677",
                            "streamingPolicyName" : "cencDrmStreaming",
                            "contentKeyPolicyName" : "cencDrmKey",
                            "generateAudioFilters" : true,
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

         DRM Locator Creation (3 DRMs)

          curl -X POST \
              'http://localhost:7071/api/EventGrid' \
              -H 'Content-Type: application/json' \
              -H 'aeg-event-type: Notification' \
              -H 'cache-control: no-cache' \
              -d '[
                    {
                        "eventType": "request.mediaservices.locator.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "containerUri": "https://gridwichproxy00sasb.blob.core.windows.net/fd7b4d3a-8f20-4744-b7a0-c26252580677",
                            "streamingPolicyName" : "multiDrmStreaming",
                            "contentKeyPolicyName" : "multiDrmKey",
                            "generateAudioFilters" : true,
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