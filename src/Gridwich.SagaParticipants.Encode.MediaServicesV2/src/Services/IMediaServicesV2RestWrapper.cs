using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Services
{
    /// <summary>
    /// Azure Media Service client provider.
    /// </summary>
    public interface IMediaServicesV2RestWrapper
    {
        /// <summary>
        /// Creates an empty asset in the requested storage account.
        /// </summary>
        /// <param name="assetName">The desired assetName.</param>
        /// <param name="accountName">The desired accountName.</param>
        /// <returns>string AssetId, Uri AssetUri</returns>
        /// <exception cref="GridwichMediaServicesV2RestException">GridwichMediaServicesV2RestException</exception>
        Task<(string AssetId, Uri AssetUri)> CreateEmptyAssetAsync(string assetName, string accountName);

        /// <summary>
        /// GetAssetNameAndUriAsync.
        /// </summary>
        /// <param name="assetId">assetId</param>
        /// <returns>string AssetName, Uri AssetUri</returns>
        Task<(string AssetName, Uri AssetUri)> GetAssetNameAndUriAsync(string assetId);

        /// <summary>
        /// CreateFileInfosAsync.
        /// </summary>
        /// <param name="assetId">assetId</param>
        /// <returns>void</returns>
        Task CreateFileInfosAsync(string assetId);

        /// <summary>
        /// GetLatestMediaProcessorAsync
        /// </summary>
        /// <param name="mediaProcessorName">mediaProcessorName</param>
        /// <returns>mediaProcessorId</returns>
        Task<string> GetLatestMediaProcessorAsync(string mediaProcessorName);

        /// <summary>
        /// Starts a job.
        /// </summary>
        /// <param name="jobName">jobName</param>
        /// <param name="processorId">processorId</param>
        /// <param name="inputAssetId">inputAssetId</param>
        /// <param name="preset">preset</param>
        /// <param name="outputAssetName">outputAssetName</param>
        /// <param name="outputAssetStorageAccountName">outputAssetStorageAccountName</param>
        /// <param name="correlationData">correlationData</param>
        /// <param name="notificationEndPointId">notificationEndPointId</param>
        /// <returns>jobId</returns>
        Task<string> CreateJobAsync(
            string jobName,
            string processorId,
            string inputAssetId,
            string preset,
            string outputAssetName,
            string outputAssetStorageAccountName,
            string correlationData,
            string notificationEndPointId);

        /// <summary>
        /// Creates or updates the notification endpoint used as a webhook for task state changes.
        /// </summary>
        /// <param name="notificationEndPointName">The name to use.</param>
        /// <param name="callbackEndpoint">The azure function endpoint, that calls DefaultMediaServicesV2CallbackService.</param>
        /// <returns>notificationEndPointId</returns>
        Task<string> GetOrCreateNotificationEndPointAsync(
            string notificationEndPointName,
            Uri callbackEndpoint);

        /// <summary>
        /// Gets the AssetFileNames in an Asset.
        /// </summary>
        /// <param name="assetId">assetId.</param>
        /// <returns>An enumerable of file paths.</returns>
        Task<IEnumerable<string>> GetAssetFilesNames(string assetId);

        /// <summary>
        /// Gets the Id and Name of the first task in a job.
        /// </summary>
        /// <param name="jobId">jobId</param>
        /// <returns>string FirstTaskId, string FirstTaskName</returns>
        Task<(string FirstTaskId, string FirstTaskName)> GetFirstTaskAsync(string jobId);

        /// <summary>
        /// Gets the Id and Name of the first input asset in a job.
        /// </summary>
        /// <param name="jobId">jobId</param>
        /// <returns>string FirstInputAssetId, string FirstInputAssetName</returns>
        Task<(string FirstInputAssetId, string FirstInputAssetName)> GetFirstInputAssetAsync(string jobId);

        /// <summary>
        /// Gets the Id and Name of the first output asset in a job.
        /// </summary>
        /// <param name="jobId">jobId</param>
        /// <returns>string FirstOutputAssetId, string FirstOutputAssetName</returns>
        Task<(string FirstOutputAssetId, string FirstOutputAssetName)> GetFirstOutputAssetAsync(string jobId);

        /// <summary>
        /// Deletes an asset by id.
        /// </summary>
        /// <param name="assetId">assetId</param>
        /// <returns>void</returns>
        Task DeleteAssetAsync(string assetId);
    }
}