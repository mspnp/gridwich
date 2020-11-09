using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Services;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.EventGridHandlers
{
    /// <summary>
    /// Handles a EncodeMediaServicesV2Create request.
    /// </summary>
    public class MediaServicesV2EncodeCreateHandler : EventGridHandlerBase<MediaServicesV2EncodeCreateHandler, RequestMediaServicesV2EncodeCreateDTO>
    {
        private const string HandlerId = "0D2FCC5A-ACC6-428A-9EBE-2A248147BBE9";
        private readonly IMediaServicesV2Encoder _mediaServicesV2Encoder;

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestEncodeMediaServicesV2Create, AllVersionList }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2EncodeCreateHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="mediaServicesV2Encoder">mediaServicesV2Encoder.</param>
        public MediaServicesV2EncodeCreateHandler(
            IObjectLogger<MediaServicesV2EncodeCreateHandler> log,
            IEventGridPublisher eventPublisher,
            IMediaServicesV2Encoder mediaServicesV2Encoder)
            : base(
                  log,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _mediaServicesV2Encoder = mediaServicesV2Encoder;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestMediaServicesV2EncodeCreateDTO eventData, string eventType)
        {
            // Call the service that will do work:
            ServiceOperationResultEncodeDispatched encodeDispatched = await _mediaServicesV2Encoder.EncodeCreateAsync(eventData).ConfigureAwait(false);

            // Convert the service operation result into a success DTO:
            return new ResponseEncodeDispatchedDTO(CustomEventTypes.ResponseEncodeMediaServicesV2Dispatched)
            {
                WorkflowJobName = encodeDispatched.WorkflowJobName,
                EncoderContext = encodeDispatched.EncoderContext,
                OperationContext = encodeDispatched.OperationContext,
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
                        "eventType": "request.encode.mediaservicesv2.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "inputs": [ {
                                "bloburi" : "https://gridwichlts00sasb.blob.core.windows.net/test00/bbb_trailer_480p.mp4"
                            }],
                            "outputContainer" : "https://gridwichsprites00sasb.blob.core.windows.net/mediaservicesv2test01/",
                            "presetName" : "SpriteAndThumbnailSetting",
                            "thumbnailTimecode" : "00:00:04:10@24",
                            "operationContext": {
                                "test": "mediaServicesV2test01",
                                "someId": 1002
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
