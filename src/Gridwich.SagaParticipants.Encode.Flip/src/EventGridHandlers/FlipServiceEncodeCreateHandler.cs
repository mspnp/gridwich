using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.Flip.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Encode.Flip.EventGridHandlers
{
    /// <summary>
    /// Handles a RequestorEncodeFlipCreate.
    /// </summary>
    public class FlipServiceEncodeCreateHandler : EventGridHandlerBase<FlipServiceEncodeCreateHandler, RequestFlipEncodeCreateDTO>
    {
        private const string HandlerId = "772A7381-5EDE-4602-B788-BBA89B211A93";
        private readonly IFlipService _flipService;

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestEncodeFlipCreate, AllVersionList }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="FlipServiceEncodeCreateHandler" /> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="flipService">The flip service.</param>
        public FlipServiceEncodeCreateHandler(
            IObjectLogger<FlipServiceEncodeCreateHandler> log,
            IEventGridPublisher eventPublisher,
            IFlipService flipService)
            : base(
                  log,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _flipService = flipService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestFlipEncodeCreateDTO eventData, string eventType)
        {
            // Call the service that will do work:
            ServiceOperationResultEncodeDispatched encodeDispatched = await _flipService.EncodeCreateAsync(eventData).ConfigureAwait(false);

            // Convert the service operation result into a success DTO:
            return new ResponseEncodeDispatchedDTO(CustomEventTypes.ResponseEncodeFlipDispatched)
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
                        "eventType": "requestor.encode.flip.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "inputs": [ {
                                "bloburi" : "https://gridwichinbox00sasb.blob.core.windows.net/unikittyclipsmall/Unikitty_Clip_small.mov"
                            }],
                            "outputContainer" : "https://gridwichproxy00sasb.blob.core.windows.net/fliptest01/",
                            "factoryName" : "gridwich-dev-flip",
                            "profiles" : "h264",
                            "parameters" : [{"foo":"bar"}],
                            "secToLive": 6000,
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
