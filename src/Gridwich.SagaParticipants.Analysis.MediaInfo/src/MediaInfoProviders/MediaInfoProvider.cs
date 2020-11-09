using System;

using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Services;
using MI = MediaInfo;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders
{
    /// <summary>
    /// Creates MediaInfo object.
    /// </summary>
    public class MediaInfoProvider : IMediaInfoProvider
    {
        private readonly IObjectLogger<MediaInfoProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoProvider"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        public MediaInfoProvider(IObjectLogger<MediaInfoProvider> logger)
        {
            _logger = logger;
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public IMediaInfoService GetMediaInfoLib(StorageClientProviderContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            IMediaInfoService mediaInfoServiceLib;
            string libraryVersion;
            try
            {
                mediaInfoServiceLib = new MediaInfoServiceWrapper(new MI.MediaInfo());
                libraryVersion = mediaInfoServiceLib.GetOption("Info_Version", "0.7.0.0;MediaInfoDLL_Example_CS;0.7.0.0");
            }
            catch (Exception e)
            {
                _logger.LogException(LogEventIds.MediaInfoLibFailedToLoad, e, "Could not load MediaInfo.dll");
                throw new GridwichMediaInfoLibException("An exception was found when loading MediaInfoLib", LogEventIds.MediaInfoLibFailedToLoad, context.ClientRequestIdAsJObject, e);
            }

            if (string.IsNullOrEmpty(libraryVersion) || libraryVersion == "Unable to load MediaInfo library")
            {
                throw new GridwichMediaInfoLibException("Unable to load MediaInfo library.", LogEventIds.MediaInfoLibFailedToLoad, context.ClientRequestIdAsJObject);
            }

            return mediaInfoServiceLib;
        }
    }
}