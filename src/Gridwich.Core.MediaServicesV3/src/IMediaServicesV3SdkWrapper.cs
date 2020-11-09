using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Rest.Azure;

namespace Gridwich.Core.MediaServicesV3
{
    /// <summary>
    /// Azure Media Services V3 client SDK Wrapper.
    /// </summary>
    public interface IMediaServicesV3SdkWrapper
    {
        /// <summary>
        /// Connect to Azure Media Services.
        /// </summary>
        /// <returns>A task<see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ConnectAsync();

        /// <summary>
        /// Create or update a Media Services asset.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        /// <param name="parameters">Asset object.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>An asset object.</returns>
        public Task<Asset> AssetCreateOrUpdateAsync(string assetName, Asset parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a Media Services asset.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>An asset object.</returns>
        public Task<Asset> AssetGetAsync(string assetName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a Media Services account.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A Media Service object.</returns>
        public Task<MediaService> MediaservicesGetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Create or update a Media Services transform.
        /// </summary>
        /// <param name="transformName">Transform name.</param>
        /// <param name="transformOutputs">Transform outputs.</param>
        /// <param name="description">Description.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Transform object.</returns>
        public Task<Transform> TransformCreateOrUpdateAsync(string transformName, IEnumerable<TransformOutput> transformOutputs, string description = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a Media Services transform.
        /// </summary>
        /// <param name="transformName">Transform name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Transform object.</returns>
        public Task<Transform> TransformGetAsync(string transformName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a Media Services Job.
        /// </summary>
        /// <param name="transformName">Transform name.</param>
        /// <param name="jobName">Job name.</param>
        /// <param name="parameters">Job object.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A Job object.</returns>
        public Task<Job> JobCreateAsync(string transformName, string jobName, Job parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a Media Services Streaming Locator.
        /// </summary>
        /// <param name="streamingLocatorName">Streaming locator name.</param>
        /// <param name="parameters">Locator parameters.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Streaming locator object.</returns>
        public Task<StreamingLocator> StreamingLocatorCreateAsync(string streamingLocatorName, StreamingLocator parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// List the Paths of a Media Services Streaming Locator.
        /// </summary>
        /// <param name="streamingLocatorName">Streaming locator name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Transform object.</returns>
        public Task<ListPathsResponse> StreamingLocatorListPathsAsync(string streamingLocatorName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a Media Services Streaming Locator.
        /// </summary>
        /// <param name="streamingLocatorName">Streaming locator name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task.</returns>
        public Task StreamingLocatorDeleteAsync(string streamingLocatorName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an asset filter.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        /// <param name="filterName">Filter name.</param>
        /// <param name="parameters">Filter parameters.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Asset filter object.</returns>
        public Task<AssetFilter> AssetFiltersCreateOrUpdateAsync(string assetName, string filterName, AssetFilter parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create or update a Media Services Content Key Policy.
        /// </summary>
        /// <param name="contentKeyPolicyName">Content key policy name.</param>
        /// <param name="options">List of content key policy options.</param>
        /// <param name="description">Description of the content key policy.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Content key policy object.</returns>
        public Task ContentKeyPolicyCreateOrUpdateAsync(string contentKeyPolicyName, IEnumerable<ContentKeyPolicyOption> options, string description = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a Media Services Content Key Policy.
        /// </summary>
        /// <param name="contentKeyPolicyName">Content key policy name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Content key policy object.</returns>
        public Task<ContentKeyPolicy> ContentKeyPolicyGetAsync(string contentKeyPolicyName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a Media Services Streaming Policy.
        /// </summary>
        /// <param name="streamingPolicyName">Streaming policy name.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Streaming policy object.</returns>
        public Task<StreamingPolicy> StreamingPolicyGetAsync(string streamingPolicyName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a Media Services Streaming Policy.
        /// </summary>
        /// <param name="streamingPolicyName">Streaming policy name.</param>
        /// <param name="parameters">Streaming policy parameters.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Content key policy object.</returns>
        public Task<StreamingPolicy> StreamingPolicyCreateAsync(string streamingPolicyName, StreamingPolicy parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists the streaming endpoints in the Media Services account.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The list of streaming endpoints.</returns>
        public Task<IPage<StreamingEndpoint>> StreamingEndpointsListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispose the Wrapper.
        /// </summary>
        void Dispose();
    }
}