using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gridwich.Core.DTO;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3
{
    /// <summary>
    /// Manages the Azure Media Services operations.
    /// </summary>
    public interface IMediaServicesV3EncodeService
    {
        /// <summary>
        /// Create an Azure Media Services v3 asset. It does not copy the blobs but create an asset from the container where are the blobs.
        /// Only the listed blobs should be in the container otherwise the method will fail. All listed blobs should be in the same container.
        /// </summary>
        /// <param name="videoSourceBlobUri">List of blobs which must be in the same storage container.</param>
        /// <returns>The created asset name.</returns>
        public Task<string> CreateOrUpdateAssetForContainerAsync(IEnumerable<Uri> videoSourceBlobUri);

        /// <summary>
        /// Gets or create a transform based on name.
        /// If the transform does not exist, a simple transform will be created. It will contain one Media Encoder task using the default MES preset.
        /// </summary>
        /// <param name="transformName">Transform name.</param>
        /// <param name="operationContext">The caller's operationContext.</param>
        /// <returns>True if successful.</returns>
        public Task CreateTransformIfNotExistByNameAsync(string transformName, JObject operationContext);

        /// <summary>
        /// Create a Azure Media Services job.
        /// </summary>
        /// <param name="transformName">Transform name to use.</param>
        /// <param name="inputAssetName">The input asset name to use as source.</param>
        /// <param name="outputAssetName">The output asset name.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="timeBasedEncode">Time information about the encode.</param>
        /// <param name="correlationData">Correlation data.</param>
        /// <param name="operationContext">The caller's operationContext.</param>
        /// <returns>True if successful.</returns>
        public Task CreateJobAsync(string transformName, string inputAssetName, string outputAssetName, string jobName, TimeBasedEncodeDTO timeBasedEncode, IDictionary<string, string> correlationData, JObject operationContext);
    }
}