using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.EventGridHandlers
{
    /// <summary>
    /// Handles a MediaServicesV2CallbackHandler event.
    /// Note: This event is triggered from the webhook callback of AMSv2
    /// </summary>
    public class MediaServicesV2CallbackHandler : EventGridHandlerBase<MediaServicesV2CallbackHandler, MediaServicesV2NotificationMessage>
    {
        private readonly IMediaServicesV2EncodeService _amsV2Service;
        private readonly IObjectLogger<MediaServicesV2CallbackHandler> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2CallbackHandler"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="amsV2Service">amsV2Service.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="eventPublisher">eventPublisher.</param>
        public MediaServicesV2CallbackHandler(
            IObjectLogger<MediaServicesV2CallbackHandler> logger,
            IMediaServicesV2EncodeService amsV2Service,
            IEventGridPublisher eventPublisher)
            : base(
                  logger,
                  eventPublisher,
                  "F7CB7E33-5731-4B48-B8ED-392BF53F3C3B",
                  new Dictionary<string, string[]>
                  {
                      { CustomEventTypes.ResponseEncodeMediaServicesV2TranslateCallback, AllVersionList }
                  })
        {
            _log = logger;
            _amsV2Service = amsV2Service;
        }

         /// <inheritdoc/>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(MediaServicesV2NotificationMessage eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var jobId = eventData.Properties["jobId"];
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("notificationMessage.jobId is invalid");
            }
            // var taskId = message.Properties["TaskId"]; // TODO: this variable does not seem to be used around here
            var operationContext = await _amsV2Service.GetOperationContextForJobAsync(jobId).ConfigureAwait(false);

            try
            {
                switch (eventData.EventType)
                {
                    case MediaServicesV2NotificationEventType.TaskStateChange:
                        return await ProcessTaskStateChangeInternalAsync(eventData, jobId, operationContext).ConfigureAwait(false);
                    case MediaServicesV2NotificationEventType.TaskProgress:
                        return GetEncodeProcessingDTO(eventData, jobId, operationContext);
                    default:
                        // TODO: what happens if the message type is not handled?
                        _log.LogEventObject(LogEventIds.MediaServicesV2UnableToHandleMessageEventType, eventData);
                        throw new NotSupportedException($"Unsupported AMSv2 event type {eventData.EventType}");
                }
            }
            catch (Exception e)
            {
                _log.LogExceptionObject(LogEventIds.MediaServicesV2MessageProcessingFailed, e, eventData);

                // TODO: ResponseEncodeFailureDTO has no data about the exception!
                return new ResponseEncodeFailureDTO
                {
                    OperationContext = operationContext,
                    EncoderContext = JObject.FromObject(eventData.Properties),
                    WorkflowJobName = jobId
                };
            }
        }

        /// <summary>
        /// Processes the task progress.
        /// </summary>
        /// <param name="message">MediaServicesV2NotificationMessage.</param>
        /// <param name="jobId">The id of the job</param>
        /// <param name="operationContext">The operation context.</param>
        /// <returns>Successfully published message.</returns>
        /// <exception cref="GridwichMediaServicesV2MessageProcessingException">When processing of the message fails.</exception>
        protected static ResponseEncodeProcessingDTO GetEncodeProcessingDTO(MediaServicesV2NotificationMessage message, string jobId, JObject operationContext)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!message.Properties.TryGetValue("lastComputedProgress", out string progressString))
            {
                throw new GridwichMediaServicesV2MessageProcessingException(message, "Could not find LastComputedProgress property in notification message",
                    LogEventIds.MediaServicesV2PropertyNotFoundInNotificationMessageError, operationContext);
            }

            if (!int.TryParse(progressString, out int progress))
            {
                throw new GridwichMediaServicesV2MessageProcessingException(message, "Could not parse progress from message",
                    LogEventIds.MediaServicesV2ParseProgressFailed, operationContext);
            }

            return new ResponseEncodeProcessingDTO(CustomEventTypes.ResponseEncodeMediaServicesV2Processing)
            {
                CurrentStatus = "Processing",
                PercentComplete = progress,
                OperationContext = operationContext,
                WorkflowJobName = jobId
            };
        }

        /// <summary>
        /// Processes the task state change.
        /// </summary>
        /// <param name="message">MediaServicesV2NotificationMessage.</param>
        /// <param name="jobId">The id of the job</param>
        /// <param name="operationContext">The operation context.</param>
        /// <returns>Successfully published message.</returns>
        protected async Task<ResponseBaseDTO> ProcessTaskStateChangeInternalAsync(MediaServicesV2NotificationMessage message, string jobId, JObject operationContext)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!message.Properties.TryGetValue("newState", out string state))
            {
                throw new GridwichMediaServicesV2MessageProcessingException(message, "Could not find NewState property in notification message",
                    LogEventIds.MediaServicesV2PropertyNotFoundInNotificationMessageError, operationContext);
            }

            if (state.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                return new ResponseEncodeScheduledDTO(CustomEventTypes.ResponseEncodeMediaServicesV2Scheduled)
                {
                    OperationContext = operationContext
                };
            }
            else if (state == "Finished")
            {
                // TODO: Conform to Success event data
                var destinationUris = await _amsV2Service.CopyOutputAssetToOutputContainerAsync(jobId).ConfigureAwait(false);
                // TODO: Log/Return false if copy failed
                await _amsV2Service.DeleteAssetsForV2JobAsync(jobId).ConfigureAwait(false);

                var outputs = new List<Output> { };
                foreach (var blobUri in destinationUris)
                {
                    outputs.Add(new Output() { BlobUri = blobUri });
                }

                return new ResponseEncodeSuccessDTO(CustomEventTypes.ResponseEncodeMediaServicesV2Success)
                {
                    OperationContext = operationContext,
                    Outputs = outputs.ToArray(),
                    EncoderContext = JObject.FromObject(message.Properties),
                    WorkflowJobName = jobId
                };
            }
            else if (state == "Error")
            {
                await _amsV2Service.DeleteAssetsForV2JobAsync(jobId).ConfigureAwait(false);

                throw new GridwichEncodeJobFailedException("Encode Job Failed", operationContext);
            }
            else if (state == "Canceled")
            {
                await _amsV2Service.DeleteAssetsForV2JobAsync(jobId).ConfigureAwait(false);

                return new ResponseEncodeCanceledDTO
                {
                    OperationContext = operationContext,
                    EncoderContext = JObject.FromObject(message.Properties),
                    WorkflowJobName = jobId
                };
            }
            else if (state == "Processing")
            {
                return new ResponseEncodeProgressDTO
                {
                    OperationContext = operationContext,
                    EncoderContext = JObject.FromObject(message.Properties),
                    WorkflowJobName = jobId
                };
            }
            else
            {
                return new ResponseEncodeUnknownStatusDTO
                {
                    OperationContext = operationContext,
                    EncoderContext = JObject.FromObject(message.Properties),
                    WorkflowJobName = jobId
                };
            }
        }

        /*
            curl -X POST \
                'https://gridwich-grw-fxn-ah.azurewebsites.net/api/EventGrid?code=QLNHI2soW1Tj4w2pqv6J9yWlZch2WkCEePmmmgq9uXhRnKYK0TaC6A==' \
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
                            "inputs": [
                                {
                                    "bloburi": "https://gridwichinbox00saah.blob.core.windows.net/71ba1aa1-f7f2-4769-acd5-86c856ce98e0/ah_test.mp4"
                                }
                            ],
                            "outputContainer": "https://gridwichproxy00saah.blob.core.windows.net/71ba1aa1-f7f2-4769-acd5-86c856ce98e0/",
                            "presetName": "SpriteOnlySetting",
                            "operationContext": {
                                "test": "mediaServicesV2test01",
                                "processId": 1002
                            }
                        },
                        "eventTime": "2019-12-06T17:23:51.4701403-05:00",
                        "metadataVersion": null,
                        "dataVersion": "1.0"
                    }
                    ]' -v
        */
    }
}