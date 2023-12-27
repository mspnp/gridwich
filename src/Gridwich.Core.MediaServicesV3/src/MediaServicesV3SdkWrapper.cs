using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Gridwich.Core.Interfaces;
using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace Gridwich.Core.MediaServicesV3
{
    /// <summary>
    /// Implementation of the Media Services V3 SDK Wrapper.
    /// </summary>
    public class MediaServicesV3SdkWrapper : IMediaServicesV3SdkWrapper, IDisposable
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly string _amsResourceGroup;
        private readonly string _amsAccountName;
        private readonly string _AmsEntraClientId;
        private readonly string _AmsEntraClientSecret;
        private readonly string _AmsEntraTenantId;
        private readonly string _amsArmEndpoint;
        private readonly string _amsSubscriptionId;
        private AzureMediaServicesClient _client;
        private static readonly string TokenType = "Bearer";

        /// <summary>
        /// Dispose the Media Services V3 client.
        /// </summary>
        /// <param name="disposing">Boolean to dispose the Media ServicesV3 client.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (_client != null)
                {
                    _client.Dispose();
                }
            }
            // free native resources
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3SdkWrapper"/> class.
        /// </summary>
        /// <param name="log">Logging object.</param>
        /// <param name="settingsProvider">Settings object.</param>
        public MediaServicesV3SdkWrapper(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _amsResourceGroup = settingsProvider.GetAppSettingsValue("AmsResourceGroup");
            _amsAccountName = settingsProvider.GetAppSettingsValue("AmsAccountName");
            _AmsEntraClientId = _settingsProvider.GetAppSettingsValue("AmsEntraClientId");
            _AmsEntraClientSecret = _settingsProvider.GetAppSettingsValue("AmsEntraClientSecret");
            _AmsEntraTenantId = _settingsProvider.GetAppSettingsValue("AmsEntraTenantId");
            _amsArmEndpoint = _settingsProvider.GetAppSettingsValue("AmsArmEndpoint");
            _amsSubscriptionId = _settingsProvider.GetAppSettingsValue("AmsSubscriptionId");
        }

        private static readonly Lazy<TokenCredential> _msiCredential = new(() =>
        {
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using DefaultAzureCredential allows for local dev by setting environment variables for the current user, provided said user
            // has the necessary credentials to perform the operations the MSI of the Function app needs in order to do its work. Including
            // interactive credentials will allow browser-based login when developing locally.

            return new DefaultAzureCredential(includeInteractiveCredentials: true);
        });

        /// <inheritdoc/>
        public async Task ConnectAsync()
        {
            var armArmAudience = "https://management.core.windows.net";
            var scopes = new[] { armArmAudience + "/.default" };
            string token;
            if (_AmsEntraClientId != null)
            {
                // Service Principal
                var app = ConfidentialClientApplicationBuilder.Create(_AmsEntraClientId)
                .WithClientSecret(_AmsEntraClientSecret)
                .WithAuthority(AzureCloudInstance.AzurePublic, _AmsEntraTenantId)
                .Build();

                var authResult = await app.AcquireTokenForClient(scopes)
                                                        .ExecuteAsync()
                                                        .ConfigureAwait(false);
                token = authResult.AccessToken;
            }
            else
            {
                // managed identity
                TokenCredential tokenCred = _msiCredential.Value;
                AccessToken accesToken = await tokenCred.GetTokenAsync(new TokenRequestContext(scopes), CancellationToken.None);
                token = accesToken.Token;
            }

            ServiceClientCredentials serviceClientCredentials = new TokenCredentials(token, TokenType);
            _client = new AzureMediaServicesClient(new Uri(_amsArmEndpoint), serviceClientCredentials)
            {
                SubscriptionId = _amsSubscriptionId,
            };
        }


        /// <inheritdoc/>
        public Task<Asset> AssetCreateOrUpdateAsync(string assetName, Asset parameters, CancellationToken cancellationToken = default) => _client.Assets.CreateOrUpdateAsync(_amsResourceGroup, _amsAccountName, assetName, parameters, cancellationToken);

        /// <inheritdoc/>
        public Task<Asset> AssetGetAsync(string assetName, CancellationToken cancellationToken = default) => _client.Assets.GetAsync(_amsResourceGroup, _amsAccountName, assetName, cancellationToken);

        /// <inheritdoc/>
        public Task<Job> JobCreateAsync(string transformName, string jobName, Job parameters, CancellationToken cancellationToken = default) => _client.Jobs.CreateAsync(_amsResourceGroup, _amsAccountName, transformName, jobName, parameters, cancellationToken);

        /// <inheritdoc/>
        public Task<MediaService> MediaservicesGetAsync(CancellationToken cancellationToken = default) => _client.Mediaservices.GetAsync(_amsResourceGroup, _amsAccountName, cancellationToken);

        /// <inheritdoc/>
        public Task<Transform> TransformCreateOrUpdateAsync(string transformName, IEnumerable<TransformOutput> transformOutputs, string description = null, CancellationToken cancellationToken = default) => _client.Transforms.CreateOrUpdateAsync(_amsResourceGroup, _amsAccountName, transformName, transformOutputs.ToList(), description, cancellationToken);

        /// <inheritdoc/>
        public Task<Transform> TransformGetAsync(string transformName, CancellationToken cancellationToken = default) => _client.Transforms.GetAsync(_amsResourceGroup, _amsAccountName, transformName, cancellationToken);

        /// <inheritdoc/>
        public Task<StreamingLocator> StreamingLocatorCreateAsync(string streamingLocatorName, StreamingLocator parameters, CancellationToken cancellationToken = default) => _client.StreamingLocators.CreateAsync(_amsResourceGroup, _amsAccountName, streamingLocatorName, parameters, cancellationToken);

        /// <inheritdoc/>
        public Task<ListPathsResponse> StreamingLocatorListPathsAsync(string streamingLocatorName, CancellationToken cancellationToken = default) => _client.StreamingLocators.ListPathsAsync(_amsResourceGroup, _amsAccountName, streamingLocatorName, cancellationToken);

        /// <inheritdoc/>
        public Task StreamingLocatorDeleteAsync(string streamingLocatorName, CancellationToken cancellationToken = default) => _client.StreamingLocators.DeleteAsync(_amsResourceGroup, _amsAccountName, streamingLocatorName, cancellationToken);

        /// <inheritdoc/>
        public Task<AssetFilter> AssetFiltersCreateOrUpdateAsync(string assetName, string filterName, AssetFilter parameters, CancellationToken cancellationToken = default) => _client.AssetFilters.CreateOrUpdateAsync(_amsResourceGroup, _amsAccountName, assetName, filterName, parameters, cancellationToken);

        /// <inheritdoc/>
        public Task ContentKeyPolicyCreateOrUpdateAsync(string contentKeyPolicyName, IEnumerable<ContentKeyPolicyOption> options, string description = null, CancellationToken cancellationToken = default) => _client.ContentKeyPolicies.CreateOrUpdateAsync(_amsResourceGroup, _amsAccountName, contentKeyPolicyName, options.ToList(), description, cancellationToken);

        /// <inheritdoc/>
        public Task<ContentKeyPolicy> ContentKeyPolicyGetAsync(string contentKeyPolicyName, CancellationToken cancellationToken = default) => _client.ContentKeyPolicies.GetAsync(_amsResourceGroup, _amsAccountName, contentKeyPolicyName, cancellationToken);

        /// <inheritdoc/>
        public Task<StreamingPolicy> StreamingPolicyGetAsync(string streamingPolicyName, CancellationToken cancellationToken = default) => _client.StreamingPolicies.GetAsync(_amsResourceGroup, _amsAccountName, streamingPolicyName, cancellationToken);

        /// <inheritdoc/>
        public Task<StreamingPolicy> StreamingPolicyCreateAsync(string streamingPolicyName, StreamingPolicy parameters, CancellationToken cancellationToken = default) => _client.StreamingPolicies.CreateAsync(_amsResourceGroup, _amsAccountName, streamingPolicyName, parameters, cancellationToken);

        /// <inheritdoc/>
        public Task<IPage<StreamingEndpoint>> StreamingEndpointsListAsync(CancellationToken cancellationToken = default) => _client.StreamingEndpoints.ListAsync(_amsResourceGroup, _amsAccountName, cancellationToken);
    }
}