using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

using Newtonsoft.Json;

namespace Gridwich.Host.FunctionApp.Functions
{
    /// <summary>
    /// The event grid function
    /// </summary>
    public class EventGridFunction
    {
        private readonly IObjectLogger<EventGridFunction> _logger;

        private readonly IEventGridDispatcher _eventDispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridFunction"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventDispatcher">The event dispatcher.</param>
        public EventGridFunction(
            IObjectLogger<EventGridFunction> logger,
            IEventGridDispatcher eventDispatcher)
        {
            _logger = logger;
            _eventDispatcher = eventDispatcher;
        }

        /// <summary>
        /// Runs the specified req.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        ///   <see cref="IActionResult"/>
        /// </returns>
        [FunctionName("EventGrid")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogEvent(LogEventIds.FunctionAppShuttingDown, LogEventIds.FunctionAppShuttingDown.Name);
                throw new OperationCanceledException("Function invoked with a canceled cancellation token.");
            }

            if (req == null)
            {
                _logger.LogEvent(LogEventIds.EventGridFunctionGotNullHttpRequest, string.Empty);
                return new BadRequestObjectResult(LogEventIds.EventGridFunctionGotNullHttpRequest.Name);
            }

            if (req.Body == null)
            {
                _logger.LogEvent(LogEventIds.EventGridFunctionGotNullHttpRequestBody, string.Empty);
                return new BadRequestObjectResult(LogEventIds.EventGridFunctionGotNullHttpRequestBody.Name);
            }

            string requestBody = string.Empty;
            try
            {
                using var sr = new StreamReader(req.Body);
                requestBody = await sr.ReadToEndAsync().ConfigureAwait(true);
            }
            catch (Exception e) when (
                e is ArgumentException ||
                e is ArgumentOutOfRangeException ||
                e is ObjectDisposedException ||
                e is InvalidOperationException)
            {
                // TODO: Tests for these expection types
                _logger.LogEventObject(LogEventIds.EventGridFunctionExceptionReadingHttpRequestBody, e);
                string msg = $"{LogEventIds.EventGridFunctionExceptionReadingHttpRequestBody} {e.Message}";
                return new BadRequestObjectResult(msg);
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogEvent(LogEventIds.EventGridFunctionGotEmptyBody, string.Empty);
                return new BadRequestObjectResult(LogEventIds.EventGridFunctionGotEmptyBody.Name);
            }

            List<EventGridEvent> eventGridEvents;
            try
            {
                eventGridEvents = JsonConvert.DeserializeObject<List<EventGridEvent>>(requestBody);
            }
            catch (JsonException e)
            {
                _logger.LogEventObject(LogEventIds.EventGridFunctionGotUnparsableBody, e);
                string msg = $"{LogEventIds.EventGridFunctionGotUnparsableBody} {e.Message}";
                return new BadRequestObjectResult(msg);
            }

            if (eventGridEvents.Count == 0)
            {
                _logger.LogEvent(LogEventIds.EventGridFunctionGotEmptyArrayAsBody, string.Empty);
                return new BadRequestObjectResult(LogEventIds.EventGridFunctionGotEmptyArrayAsBody.Name);
            }
            try
            {
                cancellationToken.Register(() =>
                {
                    _logger.LogEvent(LogEventIds.FunctionAppShuttingDown, LogEventIds.FunctionAppShuttingDown.Name);
                });

                return (IActionResult)await _eventDispatcher.DispatchEventGridEvents(eventGridEvents).ConfigureAwait(true);
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogEventObject(LogEventIds.OperationCancelException, oce);
                throw;
            }
        }
    }
}