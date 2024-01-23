using Gridwich.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders
{
    /// <summary>
    /// Generate MediaInfo reports.
    /// </summary>
    public interface IMediaInfoReportService
    {
        /// <summary>
        /// Generate a MediaInfo "Inform" report with "Complete" option set, in JSON format.
        /// </summary>
        /// <param name="blobUri">blobUri.</param>
        /// <param name="context">The current request context.</param>
        /// <returns>Inform report.</returns>
        Task<JObject> GetMediaInfoCompleteInformForUriAsync(Uri blobUri, StorageClientProviderContext context);
    }
}