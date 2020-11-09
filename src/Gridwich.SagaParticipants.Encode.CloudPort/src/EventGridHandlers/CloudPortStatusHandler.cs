using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode;
using Gridwich.SagaParticipants.Encode.CloudPort.Models;
using Gridwich.SagaParticipants.Encode.CloudPort.Services;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Newtonsoft.Json.Linq;
using Telestream.Cloud.VantageCloudPort.Model;

namespace Gridwich.SagaParticipants.Encode.CloudPort.EventGridHandlers
{
    /// <summary>
    /// EventGrid handler implementation for CloudPort encode status messages.
    /// This class will handle 4 types of events from CloudPort Encoder: video-created, encoder-progress, encoder-complete, video-complete.
    /// </summary>
    public class CloudPortStatusHandler : EventGridHandlerBase<CloudPortStatusHandler, CloudPortStatusData>
    {
        private const string HandlerId = "7E995F77-5665-4C72-ACAD-FAC9E166D5E9";

        private readonly IStorageService _storageService;
        private readonly ICloudPortService _cloudPortService;
        private static readonly string[] EventVersion = new string[] { "1.0" };

        // The eventsToHandle Dictionary is an object that binds inbound eventTypes
        // from the CloudPort notification service to the proper data structure classes.
        // These data classes (models) define the schema of the inbound Json payloads,
        // and have associated code that knows how to convert themselves into the expected
        // format for Gridwich encoder status messages.

        private static readonly Dictionary<string, string[]> EventsToHandle = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase)
        {
            { ExternalEventTypes.CloudPortWorkflowJobCreated, EventVersion },
            { ExternalEventTypes.CloudPortWorkflowJobError, EventVersion },
            { ExternalEventTypes.CloudPortWorkflowJobProgress, EventVersion },
            { ExternalEventTypes.CloudPortWorkflowJobSuccess, EventVersion }
        };


        /// <summary>
        /// Initializes a new instance of the <see cref="CloudPortStatusHandler"/> class.
        /// Gets a CloudPort complete EventGrid message and re publishes it for Requestor consumption.
        /// </summary>
        /// <param name="log">IObjectLogger logger.</param>
        /// <param name="eventGridPublisher">An EventGrid publisher to use to publish with.</param>
        /// <param name="cloudPortService">Gets the CloudPort encoder instance from the services container.</param>
        /// <param name="storageService">Storage service used for setting storage account references.</param>
        public CloudPortStatusHandler(IObjectLogger<CloudPortStatusHandler> log, IEventGridPublisher eventGridPublisher, ICloudPortService cloudPortService, IStorageService storageService)
                        : base(log, eventGridPublisher, HandlerId, EventsToHandle)
        {
            _storageService = storageService;
            _cloudPortService = cloudPortService;
        }


        /// <summary>
        /// Handles the CloudPort encode complete EventGrid message, converting it and
        /// sending it to Requestor.
        /// </summary>
        /// <param name="eventData">EventGrid object.</param>
        /// <param name="eventType">The event type.</param>
        /// <returns>True if successfully handled, false if not.</returns>
        protected override async Task<ResponseBaseDTO> DoWorkAsync(CloudPortStatusData eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));
            _ = eventType ?? throw new ArgumentNullException(nameof(eventType));

            var flipPayload = eventData.Payload;

            _ = flipPayload ?? throw new ArgumentException("FlipPayload cannot be null");

            // If the EncodeData.EventType is a Gridwich failure, then we need to convert
            // it into a generic Gridwich failure and push it up the stack.
            switch (eventType.ToLowerInvariant())
            {
                case ExternalEventTypes.CloudPortWorkflowJobError:
                    var workflowJobInfo = await _cloudPortService.GetWorkflowJobInfo(eventData.WorkflowId, eventData.Id).ConfigureAwait(false);
                    this.Log.LogEventObject(out var uriLocator, LogEventIds.EncodeCompleteFailure, new { eventData, workflowJobInfo });
                    return GetGridwichFailureDTO(workflowJobInfo.ErrorClass + ": " + workflowJobInfo.ErrorMessage, flipPayload.OperationContext, LogEventIds.EncodeCompleteFailure, uriLocator);

                case ExternalEventTypes.CloudPortWorkflowJobProgress:
                    var responseEncodeProcessingDTO = new ResponseEncodeProcessingDTO(CustomEventTypes.ResponseEncodeCloudPortProcessing)
                    {
                        PercentComplete = eventData.Progress,
                        OperationContext = flipPayload.OperationContext,
                        WorkflowJobName = eventData.Name
                    };
                    this.Log.LogEventObject(LogEventIds.CloudPortProgress, responseEncodeProcessingDTO);
                    return responseEncodeProcessingDTO;

                case ExternalEventTypes.CloudPortWorkflowJobSuccess:
                    workflowJobInfo = await _cloudPortService.GetWorkflowJobInfo(eventData.WorkflowId, eventData.Id).ConfigureAwait(false);

                    // Rifle thru all the possible output files from CloudPort and build a list.
                    List<string> listOfOutputFiles;
                    try
                    {
                        listOfOutputFiles = GetOutputFiles(workflowJobInfo);
                    }
                    catch (Exception e)
                    {
                        throw new GridwichCloudPortOutputListingException(string.Empty, flipPayload.OperationContext, e);
                    }

                    var responseEncodeSuccessDTO = new ResponseEncodeSuccessDTO(CustomEventTypes.ResponseEncodeCloudportSuccess)
                    {
                        OperationContext = flipPayload.OperationContext,
                        Outputs = listOfOutputFiles.ConvertAll(s => new Output() { BlobUri = _storageService.CreateBlobUrl(flipPayload.OutputContainer, s) }).ToArray(),
                        WorkflowJobName = eventData.Name,

                        // TODO: Uncomment this line when Cloudflare firewall rules have been addressed.
                        EncoderContext = JObject.FromObject(workflowJobInfo)
                    };

                    this.Log.LogEventObject(LogEventIds.CloudPortSuccess, responseEncodeSuccessDTO);
                    return responseEncodeSuccessDTO;

                case ExternalEventTypes.CloudPortWorkflowJobCreated:
                    return new ResponseEncodeScheduledDTO(CustomEventTypes.ResponseEncodeCloudPortScheduled)
                    {
                        OperationContext = flipPayload.OperationContext,
                        WorkflowJobName = eventData.Name
                    };

                // In theory it is not possible to reach this code.  But...
                default:
                    throw new GridwichCloudPortWorkflowJobException("Invalid event type received from CloudPort.", flipPayload.OperationContext);
            }
        }

        private static List<string> GetOutputFiles(WorkflowJob workflowJob)
        {
            var listOfOutputFiles = new List<string>();
            foreach (var job in workflowJob.ActionJobs.OrEmptyIfNull())
            {
                foreach (var encodeOutput in job.Outputs.OrEmptyIfNull())
                {
                    foreach (var outputFile in encodeOutput.Files.OrEmptyIfNull())
                    {
                        var blobName = encodeOutput.RemotePath.TrimEnd('/') + "/" + outputFile;
                        listOfOutputFiles.Add(blobName);
                    }
                }
            }
            return listOfOutputFiles;
        }
    }
}
