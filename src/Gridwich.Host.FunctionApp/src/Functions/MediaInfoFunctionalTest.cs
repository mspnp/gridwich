using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Gridwich.Host.FunctionApp.Functions
{
    /// <summary>
    /// MediaInfoLib functional test.
    /// </summary>
    public class MediaInfoFunctionalTest
    {
        private readonly IMediaInfoProvider _mediaInfoProvider;
        private readonly IObjectLogger<MediaInfoFunctionalTest> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoFunctionalTest"/> class.
        /// </summary>
        /// <param name="mediaInfoProvider">The media information provider.</param>
        /// <param name="logger">The logger.</param>
        public MediaInfoFunctionalTest(
            IMediaInfoProvider mediaInfoProvider,
            IObjectLogger<MediaInfoFunctionalTest> logger)
        {
            _mediaInfoProvider = mediaInfoProvider;
            _logger = logger;
        }


        /// <summary>
        /// Runs the specified req.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <returns>An HTTP result</returns>
        [FunctionName("MediaInfo")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req)
        {
            var errorMessage = string.Empty;

            using var mediaInfoLib = _mediaInfoProvider.GetMediaInfoLib(StorageClientProviderContext.None);
            string libraryVersion = mediaInfoLib?.GetOption("Info_Version", "0.7.0.0;MediaInfoDLL_Example_CS;0.7.0.0");

            if (string.IsNullOrEmpty(libraryVersion))
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return new NotFoundObjectResult(errorMessage);
                }

                libraryVersion = $"MediaInfo.Dll: this version of the DLL is not compatible. {req?.Host}";
                _logger.LogEvent(LogEventIds.MediaInfoIncompatibleDllVersion, libraryVersion);
                return new NotFoundObjectResult($"{libraryVersion}");
            }

            _logger.LogEvent(out var telemetryLink, LogEventIds.MediaInfoCompatibleDllVersion, libraryVersion);
            return new ContentResult
            {
                Content = $"<HTML><BODY>{libraryVersion}  {req?.Host} <a href=\"{telemetryLink}\">Link to telemetry</a></BODY></HTML>",
                ContentType = @"text/html",
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}