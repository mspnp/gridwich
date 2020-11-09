using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Services;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders
{
    /// <summary>
    /// MediaInfoProvider interface
    /// </summary>
    public interface IMediaInfoProvider
    {
        /// <summary>
        /// Gets the media information library.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <see cref="MediaInfoServiceWrapper"/>
        /// </returns>
        IMediaInfoService GetMediaInfoLib(StorageClientProviderContext context);
    }
}