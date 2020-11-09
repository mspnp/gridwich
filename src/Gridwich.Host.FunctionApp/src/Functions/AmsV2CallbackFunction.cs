using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Gridwich.Host.FunctionApp.Functions
{
    /// <summary>
    /// AmsV2CallbackFunction get the job status from Azure Media Services V2.
    /// </summary>
    public class AmsV2CallbackFunction
    {
        private readonly IObjectLogger<MediaInfoFunctionalTest> _logger;
        private readonly IEventGridPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmsV2CallbackFunction"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="publisher">publisher.</param>
        public AmsV2CallbackFunction(
            IObjectLogger<MediaInfoFunctionalTest> logger,
            IEventGridPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        /// <summary>
        /// The webhook used for AMS V2 notifications.
        /// </summary>
        /// <param name="req">web request.</param>
        /// <param name="cancellationToken">the cancellation token.</param>
        /// <returns>web response.</returns>
        [FunctionName("AmsV2Callback")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogEvent(LogEventIds.FunctionAppShuttingDown, LogEventIds.FunctionAppShuttingDown.Name);
                throw new OperationCanceledException("Function invoked with a canceled cancellation token.");
            }

            try
            {
                cancellationToken.Register(() =>
                {
                    _logger.LogEvent(LogEventIds.FunctionAppShuttingDown, LogEventIds.FunctionAppShuttingDown.Name);
                });

                _logger.LogEvent(LogEventIds.CallbackFunctionTriggered, $"C# HTTP trigger function processed a request. Path={req?.Path}");

                // TODO: not sure why we have to get the byte array.. might be legacy code
                // MemoryStream stream = new MemoryStream();
                // await req.Body.CopyToAsync(stream);
                // byte[] bodyByteArray = stream.ToArray();

                string requestBody;
                using (var sr = new StreamReader(req.Body))
                {
                    requestBody = await sr.ReadToEndAsync().ConfigureAwait(false);
                }

                if (req.Headers.TryGetValue("ms-signature", out _))
                {
                    // TODO: need to verify headers here. However not sure how to get the signing key.
                    MediaServicesV2NotificationMessage notificationMessage = JsonConvert.DeserializeObject<MediaServicesV2NotificationMessage>(requestBody);

                    var eventGridEventId = System.Guid.NewGuid().ToString();
                    var eventToPublish = new Microsoft.Azure.EventGrid.Models.EventGridEvent
                    {
                        Id = eventGridEventId,
                        Data = notificationMessage,
                        EventTime = System.DateTime.UtcNow,
                        EventType = CustomEventTypes.ResponseEncodeMediaServicesV2TranslateCallback,
                        Subject = $"/{CustomEventTypes.ResponseEncodeMediaServicesV2TranslateCallback}/{eventGridEventId}",
                        DataVersion = "1.0",
                    };

                    await _publisher.PublishEventToTopic(eventToPublish).ConfigureAwait(false);
                    _logger.LogEvent(LogEventIds.CallbackFunctionNotificationMessageProcessed, "processed notification message");
                }
                else
                {
                    _logger.LogEvent(LogEventIds.RequestIsMissingVerifyWebHookRequestSignature, "VerifyWebHookRequestSignature failed.");
                    return new BadRequestObjectResult("VerifyWebHookRequestSignature failed.");
                }

                return new OkObjectResult("OK");
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogEventObject(LogEventIds.OperationCancelException, oce);
                throw;
            }
        }
    }
}