using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode;
using Gridwich.SagaParticipants.Encode.CloudPort.Services;

namespace Gridwich.SagaParticipants.Encode.CloudPort.EventGridHandlers
{
    /// <summary>
    /// Handles CloudPort stuff.
    /// </summary>
    public class CloudPortEncodeCreateHandler : EventGridHandlerBase<CloudPortEncodeCreateHandler, RequestCloudPortEncodeCreateDTO>
    {
        private const string HandlerId = "4F33F10F-13CC-457C-AD9C-BA8A5CB5518D";
        private readonly ICloudPortService _cloudPortService;

        private static readonly Dictionary<string, string[]> AcceptedEvents =
            new Dictionary<string, string[]>
            {
                { CustomEventTypes.RequestEncodeCloudPortCreate, AllVersionList }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudPortEncodeCreateHandler"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        /// <param name="cloudPortService">cloudPortService.</param>
        public CloudPortEncodeCreateHandler(
            IObjectLogger<CloudPortEncodeCreateHandler> log,
            IEventGridPublisher eventPublisher,
            ICloudPortService cloudPortService)
            : base(
                  log,
                  eventPublisher,
                  HandlerId,
                  AcceptedEvents)
        {
            _cloudPortService = cloudPortService;
        }

        /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(RequestCloudPortEncodeCreateDTO eventData, string eventType)
        {
            // Call the service that will do work:
            ServiceOperationResultEncodeDispatched encodeDispatched = await _cloudPortService.EncodeCreateAsync(eventData).ConfigureAwait(false);

            // Convert the service operation result into a success DTO:
            return new ResponseEncodeDispatchedDTO(CustomEventTypes.ResponseEncodeCloudPortDispatched)
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
                        "eventType": "request.encode.cloudport.create",
                        "topic": "/NotUsedInTesting",
                        "id": "d5ffca18-29f8-4529-ae74-ac88ec5a7ed1",
                        "subject": "/NotUsed",
                        "data": {
                            "inputs": [ {
                                "bloburi" : "https://gridwichinbox00sasb.blob.core.windows.net/unikittyclipsmall/Unikitty_Clip_small.mov"
                            }],
                            "outputContainer" : "https://gridwichproxy00sasb.blob.core.windows.net/cloudporttest01/",
                            "workflowName" : "TestWorkflow2",
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
