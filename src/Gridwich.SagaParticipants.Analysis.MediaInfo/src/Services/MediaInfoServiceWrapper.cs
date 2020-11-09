using System;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.Services
{
    /// <summary>
    /// A wrapper for the MI.MediaInfo so that we can mock and test better.
    /// </summary>
    public class MediaInfoServiceWrapper : IMediaInfoService
    {
        private readonly global::MediaInfo.MediaInfo _mediaInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoServiceWrapper"/> class.
        /// </summary>
        /// <param name="mediaInfo">The MI.MediaInfo object.</param>
        public MediaInfoServiceWrapper(global::MediaInfo.MediaInfo mediaInfo)
        {
            _mediaInfo = mediaInfo;
        }

        /// <inheritdoc/>
        public IntPtr OpenBufferInit(long fileSize, long fileOffset)
        {
            return _mediaInfo.OpenBufferInit(fileSize, fileOffset);
        }

        /// <inheritdoc/>
        public IntPtr OpenBufferContinue(IntPtr buffer, IntPtr bufferSize)
        {
            return _mediaInfo.OpenBufferContinue(buffer, bufferSize);
        }

        /// <inheritdoc/>
        public long OpenBufferContinueGoToGet()
        {
            return _mediaInfo.OpenBufferContinueGoToGet();
        }

        /// <inheritdoc/>
        public IntPtr OpenBufferFinalize()
        {
            return _mediaInfo.OpenBufferFinalize();
        }

        /// <inheritdoc/>
        public string GetOption(string optionName)
        {
            return _mediaInfo.Option(optionName);
        }

        /// <inheritdoc/>
        public string GetOption(string optionName, string value)
        {
            return _mediaInfo.Option(optionName, value);
        }

        /// <inheritdoc/>
        public string GetInform()
        {
            return _mediaInfo.Inform();
        }

        /// <summary>
        /// Dispose the MediaInfo library instance.
        /// </summary>
        /// <param name="disposing">Boolean to dispose the MediaInfo library instance.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _mediaInfo?.Dispose();
            }
            // free native resources
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}