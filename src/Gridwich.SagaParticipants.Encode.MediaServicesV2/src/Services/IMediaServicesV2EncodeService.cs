using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Services
{
    /// <summary>
    /// Manages the Azure Media Services operations.
    /// </summary>
    public interface IMediaServicesV2EncodeService
    {
        /// <summary>
        /// Copies files and updates properties.
        /// </summary>
        /// <param name="filesToCopy">Uris of files to copy.</param>
        /// <returns>The inputAssetId.</returns>
        public Task<string> CopyFilesIntoNewAsset(IEnumerable<Uri> filesToCopy);

        /// <summary>
        /// Create a Azure Media Services job with the MediaEncoderStandard processor.
        /// </summary>
        /// <param name="inputAssetId">inputAssetId.</param>
        /// <param name="preset">The preset to be used, a known preset, or json.</param>
        /// <param name="outputContainer">The Uri to the expected output container.</param>
        /// <param name="callbackEndpoint">The callback endpoint url for V2 notifications.</param>
        /// <param name="correlationData">The correlation data to be base64UrlEncoded into the task name.</param>
        /// <returns>JobId.</returns>
        public Task<string> SubmitMesJobAsync(string inputAssetId, string preset, Uri outputContainer, Uri callbackEndpoint, IDictionary<string, string> correlationData);

        /// <summary>
        /// Provides the OperationContext that was set for the Job.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>OperationContext sent by Requestor when starting this job.</returns>
        public Task<JObject> GetOperationContextForJobAsync(string jobId);

        /// <summary>
        /// Copies the AMS.V2 created asset to the desired output container.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>The array of the destination output</returns>
        public Task<Uri[]> CopyOutputAssetToOutputContainerAsync(string jobId);

        /// <summary>
        /// Deletes both the input and output assets associated with a jobId.
        /// </summary>
        /// <param name="jobId">jobId.</param>
        /// <returns>The deletion has completed successfully.</returns>
        public Task<bool> DeleteAssetsForV2JobAsync(string jobId);
    }
}