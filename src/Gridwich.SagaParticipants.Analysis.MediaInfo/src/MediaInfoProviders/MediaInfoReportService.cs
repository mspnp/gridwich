using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders
{
    /// <summary>
    /// Generates MediaInfo reports.
    /// </summary>
    public class MediaInfoReportService : IMediaInfoReportService
    {
        /// <summary>
        /// When MediaInfo's OpenBufferContinueGoToGet returns this value, it means it wants to read forward.
        /// </summary>
        public const long MediaInfoReadForward = -1;
        private const string CompleteOptionString = "Complete";
        private const string CompleteOptionValueString = "1";
        private const string OutputOptionString = "Output";
        private const string JsonOutputValueString = "JSON";
        private readonly IMediaInfoProvider _mediaInfoProvider;
        private readonly IStorageService _storageService;
        private readonly IObjectLogger<MediaInfoReportService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoReportService"/> class.
        /// </summary>
        /// <param name="mediaInfoProvider">mediaInfoProvider.</param>
        /// <param name="storageService">storageService.</param>
        /// <param name="logger">logger.</param>
        public MediaInfoReportService(
            IMediaInfoProvider mediaInfoProvider,
            IStorageService storageService,
            IObjectLogger<MediaInfoReportService> logger)
        {
            _mediaInfoProvider = mediaInfoProvider;
            _storageService = storageService;
            _logger = logger;
        }

        public enum MediaInfoStatus
        {
            None = 0x00,
            Accepted = 0x01,
            Filled = 0x02,
            Updated = 0x04,
            Finalized = 0x08,
        }

        /// <inheritdoc/>
        public async Task<JObject> GetMediaInfoCompleteInformForUriAsync(Uri blobUri, StorageClientProviderContext context)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));

            _ = context ?? throw new ArgumentNullException(nameof(context));
            // Get MediaInfo instance
            var mediaInfoLib = _mediaInfoProvider.GetMediaInfoLib(context);

            // Get contentLength
            var blobProperties = await _storageService.GetBlobPropertiesAsync(blobUri, context).ConfigureAwait(false);
            var contentLength = blobProperties.ContentLength;

            if (contentLength == 0)
            {
                _logger.LogEventObject(LogEventIds.InvalidBlobContentLength, blobUri);
                throw new GridwichMediaInfoLibException($"Content length 0 for blob uri {blobUri}", LogEventIds.InvalidBlobContentLength, context.ClientRequestIdAsJObject);
            }

            long desiredOffset = 0;
            do
            {
                // Get content for current desiredOffset

                var cachedHttpRangeContent = await _storageService.GetOrDownloadContentAsync(blobUri, desiredOffset, IStorageService.UseDefaultLength, context).ConfigureAwait(false);

                if (cachedHttpRangeContent == null)
                {
                    _logger.LogEventObject(LogEventIds.MediaInfoInvalidContent, new { blobUri, desiredOffset });
                    throw new GridwichMediaInfoInvalidContentException(blobUri, desiredOffset, "The HTTP range obtained is invalid",
                        context.ClientRequestIdAsJObject);
                }

                byte[] mediaInfoBuffer;
                try
                {
                    // Copy to local buffer.
                    mediaInfoBuffer = cachedHttpRangeContent.CachedMemoryStream.ToArray();

                    // Tell MediaInfo what offset this represents in the file:
                    mediaInfoLib.OpenBufferInit(contentLength, cachedHttpRangeContent.CachedHttpRange.Offset);
                }
                catch (Exception e)
                {
                    _logger.LogExceptionObject(LogEventIds.MediaInfoLibOpenBufferInitFailed, e, new { blobUri, httpRange = cachedHttpRangeContent.CachedHttpRange });
                    throw new GridwichMediaInfoLibUnexpectedException(blobUri, "MediaInfoLib threw an unexpected exception on buffer initialization",
                        LogEventIds.MediaInfoLibOpenBufferInitFailed, context.ClientRequestIdAsJObject, e);
                }

                // Pin and send the buffer to MediaInfo, who will read and parse it:
                MediaInfoStatus result;
                try
                {
                    GCHandle gcHandle = GCHandle.Alloc(mediaInfoBuffer, GCHandleType.Pinned);
                    IntPtr addrOfBuffer = gcHandle.AddrOfPinnedObject();
                    result = (MediaInfoStatus)mediaInfoLib.OpenBufferContinue(addrOfBuffer, (IntPtr)mediaInfoBuffer.Length);
                    gcHandle.Free();
                }
                catch (Exception e) when (
                    e is ArgumentException ||
                    e is InvalidOperationException)
                {
                    _logger.LogExceptionObject(LogEventIds.MediaInfoLibOpenBufferContinueFailed, e, new { blobUri, httpRange = cachedHttpRangeContent.CachedHttpRange });
                    throw new GridwichMediaInfoLibUnexpectedException(blobUri, "MediaInfoLib threw an unexpected exception on open buffer continuation",
                        LogEventIds.MediaInfoLibOpenBufferContinueFailed, context.ClientRequestIdAsJObject, e);
                }

                // Check if MediaInfo is done.
                if ((result & MediaInfoStatus.Finalized) == MediaInfoStatus.Finalized)
                {
                    _logger.LogEventObject(LogEventIds.MediaInfoFileReadFinalized, new { blobUri, httpRange = cachedHttpRangeContent.CachedHttpRange });
                    break;
                }

                try
                {
                    // Test if MediaInfo requests to go elsewhere
                    desiredOffset = mediaInfoLib.OpenBufferContinueGoToGet();
                }
                catch (Exception e)
                {
                    _logger.LogExceptionObject(LogEventIds.MediaInfoLibOpenBufferContinueGoToGetFailed, e, new { blobUri, desiredOffset, httpRange = cachedHttpRangeContent.CachedHttpRange });
                    throw new GridwichMediaInfoLibUnexpectedException(blobUri, "MediaInfoLib threw an unexpected exception on OpenBufferContinueGoToGet operation",
                        LogEventIds.MediaInfoLibOpenBufferContinueGoToGetFailed, context.ClientRequestIdAsJObject, e);
                }

                if (desiredOffset == contentLength)
                {
                    // MediaInfo requested EndOfFile:
                    _logger.LogEventObject(LogEventIds.MediaInfoRequestedEndOfFile, new { blobUri, desiredOffset });
                    break;
                }
                else if (desiredOffset == MediaInfoReadForward)
                {
                    // MediaInfo wants to continue reading forward
                    // Adjust the byte-range request offset
                    desiredOffset = (long)(cachedHttpRangeContent.CachedHttpRange.Offset + cachedHttpRangeContent.CachedHttpRange.Length);
                    _logger.LogEventObject(LogEventIds.MediaInfoReadNewRangeRequested, new { blobUri, desiredOffset });
                }
                else
                {
                    // Specific seek requested
                    _logger.LogEventObject(LogEventIds.MediaInfoSeekRequested, new { blobUri, desiredOffset });
                }

                if (desiredOffset >= contentLength)
                {
                    _logger.LogEventObject(LogEventIds.MediaInfoMismatchInDesiredOffset, new { blobUri, desiredOffset });
                    break;
                }
            }
            while (true);

            // This is the end of the stream, MediaInfo must finish some work
            mediaInfoLib.OpenBufferFinalize();

            // Use MediaInfoLib as needed
            mediaInfoLib.GetOption(CompleteOptionString, CompleteOptionValueString);
            mediaInfoLib.GetOption(OutputOptionString, JsonOutputValueString);
            var report = mediaInfoLib.GetInform();

            if (string.IsNullOrEmpty(report))
            {
                _logger.LogEventObject(LogEventIds.InvalidMediaInfoLibReport, blobUri);
                throw new GridwichMediaInfoLibException("MediaInfoLib was null.", LogEventIds.InvalidMediaInfoLibReport, context.ClientRequestIdAsJObject);
            }

            return JsonHelpers.JsonToJObject(report, true); // return "as-is" with MediaInfoLib member casing.
        }
    }
}