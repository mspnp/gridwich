using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3.EventGridHandlers
{
    /// <summary>Handles a EncodeMediaServicesV3Create request.</summary>
    public class MediaServicesV3EncodeCreateHandler : EventGridHandlerBase<MediaServicesV3EncodeCreateHandler, RequestMediaServicesV3EncodeCreateDTO>
    {
        private const string HandlerId = "EE2FCC5A-ACC6-428A-9EBE-2A248147BBE9";
        private readonly IMediaServicesV3Encoder _mediaServicesV3Encoder;

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestEncodeMediaServicesV3Create, AllVersionList }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3EncodeCreateHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="mediaServicesV3Encoder">mediaServicesV3Encoder.</param>
        public MediaServicesV3EncodeCreateHandler(
            IObjectLogger<MediaServicesV3EncodeCreateHandler> log,
            IEventGridPublisher eventPublisher,
            IMediaServicesV3Encoder mediaServicesV3Encoder)
            : base(
                  log,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _mediaServicesV3Encoder = mediaServicesV3Encoder;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestMediaServicesV3EncodeCreateDTO eventData, string eventType)
        {
            // Call the service that will do work:
            ServiceOperationResultEncodeDispatched encodeDispatched = await _mediaServicesV3Encoder.EncodeCreateAsync(eventData).ConfigureAwait(false);

            // Convert the service operation result into a success DTO:
            return new ResponseEncodeDispatchedDTO(CustomEventTypes.ResponseEncodeMediaServicesV3Dispatched)
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
                        "eventType": "request.encode.mediaservicesv3.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "inputs": [ {
                                "bloburi" : "https://gridwichinbox00sasb.blob.core.windows.net/unikittyclipsmall/Unikitty_Clip_small.mov"
                            }],
                            "outputContainer" : "https://gridwichinboxpxy00sasb.blob.core.windows.net/audio-copy-video-mbr-01/",
                            "transformName" : "audio-copy-video-mbr",
                            "operationContext": {
                                "test": "audio-copy-video-mbr",
                                "someId": 1
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
