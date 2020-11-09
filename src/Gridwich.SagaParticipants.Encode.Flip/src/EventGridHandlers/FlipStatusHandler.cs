using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gridwich.Core.Bases;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.Flip.Models;
using Gridwich.SagaParticipants.Encode.Flip.Services;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Flip.EventGridHandlers
{
    /// <summary>
    /// EventGrid handler implementation for Flip encode status messages.
    /// This class will handle 4 types of events from Flip Encoder: video-created, encoder-progress, encoder-complete, video-complete.
    /// </summary>
    public class FlipStatusHandler : EventGridHandlerBase<FlipStatusHandler, FlipStatusData>
    {
        private const string HandlerId = "99995F77-5665-4C72-ACAD-FAC9DEADBEEF";

        // The eventsToHandle Dictionary is an object that binds inbound eventTypes
        // from the Flip notification service to the proper data structure classes.
        // These data classes (models) define the schema of the inbound Json payloads,
        // and have associated code that knows how to convert themselves into the
        // expected format for Gridwich encoder status messages.

        private static readonly Dictionary<string, Type> EventsToHandle = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase)
        {
            { ExternalEventTypes.FlipEncodingComplete, typeof(FlipEncodingCompleteData) },
            { ExternalEventTypes.FlipEncodingProgress, typeof(FlipEncodingProgressData) },
            { ExternalEventTypes.FlipVideoCreated, typeof(FlipVideoCreatedData) },
            // { ExternalEventTypes.FlipVideoEncoded, typeof(FlipVideoEncodedData) }  Ignore this message for now.  Might need for multiple profile encodes.
        };

        private static readonly Dictionary<string, string[]> AcceptedEvents = EventsToHandle.ToDictionary(i => i.Key, j => AllVersionList);
        private readonly IStorageService _storageService;
        private readonly IFlipService _flipService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlipStatusHandler" /> class.
        /// Gets a Flip complete EventGrid message and re publishes it for Requestor consumption.
        /// </summary>
        /// <param name="log">IObjectLogger logger.</param>
        /// <param name="eventGridPublisher">An EventGrid publisher to use to publish with.</param>
        /// <param name="flipService">The flip service.</param>
        /// <param name="storageService">The storage service.</param>
        public FlipStatusHandler(IObjectLogger<FlipStatusHandler> log, IEventGridPublisher eventGridPublisher, IFlipService flipService, IStorageService storageService)
                        : base(log, eventGridPublisher, HandlerId, AcceptedEvents)
        {
            _storageService = storageService;
            _flipService = flipService;
        }

        /// <inheritdoc/>
        protected override FlipStatusData GetEventData(EventGridEvent eventGridEvent)
        {
            _ = eventGridEvent ?? throw new ArgumentNullException(nameof(eventGridEvent));

            if (EventsToHandle.TryGetValue(eventGridEvent.EventType, out var eventClass))
            {
                return JsonConvert.DeserializeObject(eventGridEvent.Data.ToString(), eventClass) as FlipStatusData;
            }

            // TODO: Fix storage context
            throw new GridwichFlipNotHandledException($"The event type {eventGridEvent.EventType} cannot be parsed by this handler.", null);
        }

        /// <inheritdoc/>
        protected override Task<ResponseBaseDTO> DoWorkAsync(FlipStatusData eventData, string eventType)
        {
            _ = eventData ?? throw new ArgumentNullException(nameof(eventData));

            var encodeData = eventData.ToGridwichEncodeData();

            // TODO: Do we care about subject? We can pipe this back as part of the DTO
            // var subject = $"flipencode/{eventData.ServiceName}/{eventData.OriginalFilename}";

            encodeData.WorkflowJobName = eventData.VideoId;

            switch (encodeData)
            {
                case ResponseEncodeFailureDTO _:
                    // If the EncodeData.EventType is an Gridwich Failure, then we
                    // need to convert it into a generic Gridwich failure and push it up the stack.
                    var flipCompleteStatus = (FlipEncodingCompleteData)eventData;
                    var encodeInfo = _flipService.GetEncodeInfo(flipCompleteStatus);
                    this.Log.LogEventObject(out Uri uriLocator, LogEventIds.EncodeCompleteFailure, new { encodeData, encodeInfo });
                    return Task.FromResult<ResponseBaseDTO>(GetGridwichFailureDTO(encodeInfo.ErrorClass + ": " + encodeInfo.ErrorMessage, encodeData.OperationContext, LogEventIds.EncodeCompleteFailure.Id, uriLocator));

                case ResponseEncodeSuccessDTO encodeSuccessData:

                    flipCompleteStatus = (FlipEncodingCompleteData)eventData;
                    encodeInfo = _flipService.GetEncodeInfo(flipCompleteStatus);
                    encodeSuccessData.Outputs = encodeInfo.Files.ConvertAll(s => new Output() { BlobUri = _storageService.CreateBlobUrl(encodeSuccessData.OutputContainer, s).ToString() }).ToArray();

                    // TODO: Uncomment this line when Cloudflare firewall rules have been addressed.
                    encodeSuccessData.EncoderContext = JObject.FromObject(encodeInfo);
                    return Task.FromResult<ResponseBaseDTO>(encodeSuccessData);

                default:
                    return Task.FromResult<ResponseBaseDTO>(encodeData);
            }
        }
    }
}
