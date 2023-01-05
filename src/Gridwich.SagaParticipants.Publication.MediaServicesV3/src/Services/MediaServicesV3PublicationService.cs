using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Azure.Storage.Blobs;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies;
using Gridwich.Services.Core.Exceptions;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Services
{
    /// <summary>
    /// Default implementation of <see cref="IMediaServicesV3PublicationService"/>.
    /// </summary>
    public class MediaServicesV3PublicationService : MediaServicesV3BaseService, IMediaServicesV3PublicationService
    {
        // Instantiate a Singleton of the Semaphore with a value of 1.
        // This means that only 1 thread can be granted access at a time to update the content key policy.
        // We use Semaphore and not Mutex because we need use Async calls.
        private static readonly SemaphoreSlim SemaphoreUpdateContentKeyPolicy = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim SemaphoreCreateStreamingPolicy = new SemaphoreSlim(1, 1);

        private static readonly Dictionary<StreamingPolicyStreamingProtocol, string> ExtensionPerProtocol =
            new Dictionary<StreamingPolicyStreamingProtocol, string>()
            {
                { StreamingPolicyStreamingProtocol.Dash, ".mpd" },
                { StreamingPolicyStreamingProtocol.Hls, ".m3u8" },
            };

        private readonly IMediaServicesV3ContentKeyPolicyService _mediaServicesV3ContentKeyPolicyService;
        private readonly IMediaServicesV3CustomStreamingPolicyService _mediaServicesV3StreamingPolicyService;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;

        private readonly string environmentType = EnvironmentTypeConstants.EnvironmentTypeUnspecified;
        private readonly bool enableContentKeyPolicyAutomaticUpdate = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3PublicationService"/> class.
        /// </summary>
        /// <param name="mediaServicesV3ContentKeyPolicyService">Content key policy service.</param>
        /// <param name="mediaServicesV3StreamingPolicyService">Streaming policy service.</param>
        /// <param name="mediaServicesV3SdkWrapper">Media Services V3 Wrapper object.</param>
        /// <param name="storageService">Storage service.</param>
        /// <param name="settingsProvider">Settings Provider service.</param>
        /// <param name="log">Logger.</param>
        public MediaServicesV3PublicationService(
            IMediaServicesV3ContentKeyPolicyService mediaServicesV3ContentKeyPolicyService,
            IMediaServicesV3CustomStreamingPolicyService mediaServicesV3StreamingPolicyService,
            IMediaServicesV3SdkWrapper mediaServicesV3SdkWrapper,
            IStorageService storageService,
            ISettingsProvider settingsProvider,
            IObjectLogger<MediaServicesV3BaseService> log)
            : base(mediaServicesV3SdkWrapper, log)
        {
            _mediaServicesV3ContentKeyPolicyService = mediaServicesV3ContentKeyPolicyService;
            _mediaServicesV3StreamingPolicyService = mediaServicesV3StreamingPolicyService;
            _settingsProvider = settingsProvider;
            _storageService = storageService;

            environmentType = _settingsProvider?.GetAppSettingsValue(EnvironmentTypeConstants.EnvironmentTypeSettingName);
            enableContentKeyPolicyAutomaticUpdate = _settingsProvider?.GetAppSettingsValue("AmsDrmEnableContentKeyPolicyUpdate")?.ToUpperInvariant() == "TRUE";
        }

        /// <summary>
        /// Parses the protection string into a PredefinedStreamingPolicy for clear content, or a custom policy for DRM content.
        /// </summary>
        /// <param name="streamingPolicyName">Name of the streaming policy in Gridwich.</param>
        /// <returns>
        /// Name of the streaming policy in the AMS account.
        /// </returns>
        /// <exception cref="ArgumentNullException">streamingPolicyName</exception>
        /// <exception cref="GridwichPublicationStreamingPolicyNotSupportedException">Only streamingPolicy==clearStreamingOnly, streamingPolicy==multiDrmStreaming or streamingPolicy==cencDrmStreaming is supported.</exception>
        /// <exception cref="GridwichPublicationStreamingPolicyException">AMS v3 Publish. Error when creating the streaming policy.</exception>
        private async Task<string> GetStreamingPolicyAmsNameAsync(string streamingPolicyName)
        {
            // the name of the policy in the account.
            string streamingPolicyAmsName;

            if (string.IsNullOrWhiteSpace(streamingPolicyName))
            {
                throw new ArgumentNullException(nameof(streamingPolicyName));
            }

            if (streamingPolicyName.ToUpperInvariant() != "CLEARSTREAMINGONLY" && streamingPolicyName.ToUpperInvariant() != "CENCDRMSTREAMING" && streamingPolicyName.ToUpperInvariant() != "MULTIDRMSTREAMING")
            {
                var msg = "Only streamingPolicy==clearStreamingOnly, streamingPolicy==multiDrmStreaming or streamingPolicy==cencDrmStreaming is supported.";
                throw new GridwichPublicationStreamingPolicyNotSupportedException(
                    streamingPolicyName,
                    msg);
            }

            if (streamingPolicyName.ToUpperInvariant() == "CLEARSTREAMINGONLY")
            {
                if (environmentType?.ToUpperInvariant() != EnvironmentTypeConstants.EnvironmentTypeDevelopment)
                {
                    var msg = $"clearStreamingOnly is only allowed in the {EnvironmentTypeConstants.EnvironmentTypeDevelopment} environment, check value of {EnvironmentTypeConstants.EnvironmentTypeSettingName}.";
                    throw new GridwichPublicationStreamingPolicyNotSupportedException(
                        streamingPolicyName,
                        msg);
                }

                streamingPolicyAmsName = PredefinedStreamingPolicy.ClearStreamingOnly;
            }
            else
            {
                // MULTIDRMSTREAMING or CENCDRMSTREAMING (custom policy)

                // Attempt to find the streaming policy in the Media Services V3 Dictionary
                var mediaServicesV3StreamingPolicyServicePolicy = _mediaServicesV3StreamingPolicyService.GetCustomStreamingPolicyFromMemory(streamingPolicyName);
                // Check to see if the streaming policy was located
                if (mediaServicesV3StreamingPolicyServicePolicy == null)
                {
                    var msg = $"The streaming policy named {streamingPolicyName} could not be located in the list of valid streaming policies in Media Services V3.";
                    throw new GridwichPublicationStreamingPolicyNotSupportedException(
                        streamingPolicyName,
                        msg);
                }
                else
                {
                    // let's find the real name of the policy which should be in the AMS account
                    streamingPolicyAmsName = mediaServicesV3StreamingPolicyServicePolicy.NameInAmsAccount;
                }

                // let's try to find the policy in AMS
                StreamingPolicy streamingPolicy;
                bool createStreamingPolicy = false;
                try
                {
                    streamingPolicy = await this.MediaServicesV3SdkWrapper.StreamingPolicyGetAsync(streamingPolicyAmsName).ConfigureAwait(false);
                }
                catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    createStreamingPolicy = true;
                }
                catch (Exception ex)
                {
                    var msg = "AMS v3 Publish. Error when getting the streaming policy.";
                    throw new GridwichPublicationStreamingPolicyException(
                        streamingPolicyAmsName,
                        msg,
                        ex);
                }

                // If the streaming policy does not exist, let's create it.
                if (createStreamingPolicy)
                {
                    try
                    {
                        // Asynchronously wait to enter the Semaphore. We want to avoid two or more creations of the policy at the same time.
                        await SemaphoreCreateStreamingPolicy.WaitAsync().ConfigureAwait(false);

                        // Pattern : Double-checked locking
                        createStreamingPolicy = false;
                        try
                        {
                            streamingPolicy = await this.MediaServicesV3SdkWrapper.StreamingPolicyGetAsync(streamingPolicyAmsName).ConfigureAwait(false);
                        }
                        catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            createStreamingPolicy = true;
                        }
                        catch (Exception ex)
                        {
                            var msg = "AMS v3 Publish. Error when getting the streaming policy.";
                            throw new GridwichPublicationStreamingPolicyException(
                                streamingPolicyAmsName,
                                msg,
                                ex);
                        }
                        if (createStreamingPolicy)
                        {
                            try
                            {
                                await this.MediaServicesV3SdkWrapper.StreamingPolicyCreateAsync(streamingPolicyAmsName, mediaServicesV3StreamingPolicyServicePolicy.StreamingPolicy).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                var msg = "AMS v3 Publish. Error when creating the streaming policy.";
                                throw new GridwichPublicationStreamingPolicyException(
                                    streamingPolicyAmsName,
                                    msg,
                                    ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = "AMS v3 Publish. Error when creating the streaming policy.";
                        throw new GridwichPublicationStreamingPolicyException(
                            streamingPolicyAmsName,
                            msg,
                            ex);
                    }
                    finally
                    {
                        SemaphoreCreateStreamingPolicy.Release();
                    }
                }
            }

            return streamingPolicyAmsName;
        }

        /// <summary>
        /// Create (or get) an input asset from the existing blob container uri.
        /// </summary>
        /// <param name="containerUri">The containerUri for this asset.</param>
        /// <returns>The asset name.</returns>
        /// <exception cref="ArgumentNullException">containerUri</exception>
        /// <exception cref="GridwichPublicationCreateUpdateAssetException">AMS v3 Publish. Error when creating/getting asset.</exception>
        private async Task<string> GetAssetNameAsync(Uri containerUri)
        {
            if (containerUri == null)
            {
                throw new ArgumentNullException(nameof(containerUri));
            }

            string assetName;
            try
            {
                assetName = await CreateOrUpdateAssetForContainerAsync(Enumerable.Empty<Uri>().Append(containerUri)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 Publish. Error when creating/getting asset.";
                throw new GridwichPublicationCreateUpdateAssetException(
                    containerUri,
                    msg,
                    ex);
            }

            return assetName;
        }

        /// <summary>
        /// Gets the list of filter names to be applied to this asset, given filtering parameters.
        /// </summary>
        /// <param name="startTime">The optional start time for filtering.</param>
        /// <param name="endTime">The optional end time for filtering.</param>
        /// <returns>The list of filter names to be applied to this asset.</returns>
        private async Task<List<string>> GetListFiltersAsync(TimeSpan? startTime, TimeSpan? endTime, string uniqueness, string assetName)
        {
            List<string> listFilters = null;
            if (startTime != null || endTime != null)
            {
                string filterName = "filter-time-" + uniqueness;
                PresentationTimeRange presTimeRange = new PresentationTimeRange(
                    startTime?.Ticks,
                    endTime?.Ticks,
                    null,
                    null,
                    TimeSpan.TicksPerSecond);
                listFilters = new List<string>() { filterName };

                try
                {
                    AssetFilter newAssetFilter = await this.MediaServicesV3SdkWrapper.AssetFiltersCreateOrUpdateAsync(
                       assetName,
                       filterName,
                       new AssetFilter()
                       {
                           PresentationTimeRange = presTimeRange
                       }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var msg = "AMS v3 Publish. Error when creating asset filter.";
                    var exceptionToThrow = new GridwichPublicationAssetFilterCreateException(
                        assetName,
                        msg,
                        ex);

                    // Providing additional specific details:
                    exceptionToThrow.Data.Add("startTime", startTime?.ToString());
                    exceptionToThrow.Data.Add("endTime", endTime?.ToString());

                    throw exceptionToThrow;
                }
            }

            return listFilters;
        }

        /// <summary>
        /// Creates a filter which includes all the tracks in the list.
        /// </summary>
        /// <param name="assetName">The asset name.</param>
        /// <param name="filterName">The desired filter name.</param>
        /// <param name="tracksToKeep">The list of tracks to keep.</param>
        private async Task CreateTrackFilterAsync(string assetName, string filterName, List<TrackInfo> tracksToKeep)
        {
            List<FilterTrackSelection> includedTracks = new List<FilterTrackSelection>();

            foreach (TrackInfo track in tracksToKeep)
            {
                var conditions = new List<FilterTrackPropertyCondition>()
                        {
                            new FilterTrackPropertyCondition(FilterTrackPropertyType.Type, track.TrackType, FilterTrackPropertyCompareOperation.Equal),
                            new FilterTrackPropertyCondition(FilterTrackPropertyType.Name, track.TrackName, FilterTrackPropertyCompareOperation.Equal)
                        };

                includedTracks.Add(new FilterTrackSelection(conditions));
            }

            AssetFilter assetFilterParams = new AssetFilter(tracks: includedTracks);

            try
            {
                AssetFilter newAssetFilter = await this.MediaServicesV3SdkWrapper.AssetFiltersCreateOrUpdateAsync(
                   assetName,
                   filterName,
                   assetFilterParams)
                   .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 Publish. Error when creating asset filter.";
                var exceptionToThrow = new GridwichPublicationAssetFilterCreateException(
                    assetName,
                    msg,
                    ex);

                // Providing additional specific details:
                exceptionToThrow.SafeAddToData("filterName", filterName);

                throw exceptionToThrow;
            }
        }

        /// <summary>
        /// Creates the locator with these params.
        /// </summary>
        /// <param name="locatorName">The locator name to create.</param>
        /// <param name="assetName">assetName.</param>
        /// <param name="listFilters">listFilters to apply, if any.</param>
        /// <param name="streamingPolicy">The streamingPolicy to apply.</param>
        /// <param name="defaultContentKeyPolicyName">The defaultContentKeyPolicyName, if any, to apply.</param>
        /// <returns>The new StreamingLocator.</returns>
        private async Task<StreamingLocator> CreateLocatorAsync(string locatorName, string assetName, List<string> listFilters, string streamingPolicy, string defaultContentKeyPolicyName)
        {
            // Let's create the locator
            StreamingLocator newLocator;
            try
            {
                newLocator = await this.MediaServicesV3SdkWrapper.StreamingLocatorCreateAsync(
                    locatorName,
                    new StreamingLocator()
                    {
                        AssetName = assetName,
                        DefaultContentKeyPolicyName = defaultContentKeyPolicyName,
                        Filters = listFilters,
                        StreamingPolicyName = streamingPolicy,
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 Publish. Error when creating locator.";
                throw new GridwichPublicationLocatorCreationException(
                    assetName,
                    msg,
                    ex);
            }
            return newLocator;
        }

        /// <summary>
        /// Enumerates and selects the StreamingEndpoint to be used.
        /// </summary>
        /// <returns>StreamingEndpoint.</returns>
        private async Task<StreamingEndpoint> GetSelectedStreamingEndpointAsync()
        {
            // List the Streaming Endpoints
            List<StreamingEndpoint> streamingEndpoints = null;
            try
            {
                IPage<StreamingEndpoint> streamingEndpointPage = await this.MediaServicesV3SdkWrapper.StreamingEndpointsListAsync().ConfigureAwait(false);
                streamingEndpoints = streamingEndpointPage.ToList();
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 Publish. Error when listing the streaming endpoints in the account.";
                throw new GridwichPublicationStreamingEndpointsListException(
                    msg,
                    ex);
            }

            // let's not use the default if there is another streaming endpoint
            StreamingEndpoint selectedStreamingEndpoint = null;
            if (streamingEndpoints.Count == 1)
            {
                selectedStreamingEndpoint = streamingEndpoints.First();
            }
            else if (streamingEndpoints.Any())
            {
                selectedStreamingEndpoint = streamingEndpoints.First(h => h.Name != "default");
            }

            return selectedStreamingEndpoint;
        }

        /// <summary>
        /// Gets the list of streaming paths for this locator.
        /// </summary>
        /// <param name="locatorName">locatorName.</param>
        /// <returns>ListPathsResponse.</returns>
        private async Task<ListPathsResponse> GetListPathResponseAsync(string locatorName)
        {
            ListPathsResponse pathResponse;
            try
            {
                pathResponse = await this.MediaServicesV3SdkWrapper.StreamingLocatorListPathsAsync(locatorName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 Publish. Error when listing the streaming paths.";
                throw new GridwichPublicationListPathsException(
                    locatorName,
                    msg,
                    ex);
            }

            return pathResponse;
        }

        /// <summary>
        /// This function returns a Dash or HLS Uri from the pathResponse.
        /// It also adds .mpd or .m3u8 at the end of the URL for a better compatibility with media players.
        /// </summary>
        /// <param name="pathResponse">Response from AMS on list paths.</param>
        /// <param name="selectedStreamingEndpoint">The selected streaming endpoint.</param>
        /// <param name="protocol">The protocol selected.</param>
        /// <returns>The streaming Uri, if available, or null.</returns>
        private static Uri GetStreamingUri(ListPathsResponse pathResponse, StreamingEndpoint selectedStreamingEndpoint, StreamingPolicyStreamingProtocol protocol)
        {
            string uristr = pathResponse.StreamingPaths.Where(p => p.StreamingProtocol == protocol).FirstOrDefault()?.Paths.FirstOrDefault();
            if (uristr != null)
            {
                uristr = "https://" + selectedStreamingEndpoint.HostName + uristr;
                if (!uristr.EndsWith(ExtensionPerProtocol[protocol]))
                {
                    uristr += ExtensionPerProtocol[protocol];
                }
            }

            return uristr != null ? new Uri(uristr) : null;
        }

        /// <inheritdoc/>
        public async Task<ServiceOperationResultMediaServicesV3LocatorCreate> LocatorCreateAsync(
            Uri containerUri,
            string streamingPolicyName,
            string contentKeyPolicyName,
            TimeBasedFilterDTO timeBasedFilterInfo,
            JObject operationContext,
            bool generateAudioFilters)
        {
            TimeSpan? startTimeSpan = null;
            TimeSpan? endTimeSpan = null;
            if (!(timeBasedFilterInfo is null))
            {
                if (timeBasedFilterInfo.StartSeconds < 0)
                {
                    throw new GridwichTimeParameterException(nameof(timeBasedFilterInfo.StartSeconds), timeBasedFilterInfo.StartSeconds, "Must be above zero.");
                }
                startTimeSpan = TimeSpan.FromSeconds(timeBasedFilterInfo.StartSeconds);

                if (timeBasedFilterInfo.EndSeconds < 0)
                {
                    throw new GridwichTimeParameterException(nameof(timeBasedFilterInfo.EndSeconds), timeBasedFilterInfo.EndSeconds, "Must be above zero.");
                }
                endTimeSpan = TimeSpan.FromSeconds(timeBasedFilterInfo.EndSeconds);
            }

            await ConnectAsync().ConfigureAwait(false);

            string streamingPolicy = await GetStreamingPolicyAmsNameAsync(streamingPolicyName).ConfigureAwait(false);

            string assetName = await GetAssetNameAsync(containerUri).ConfigureAwait(false);

            string uniqueness = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 11);
            List<string> listFilters = await GetListFiltersAsync(startTimeSpan, endTimeSpan, uniqueness, assetName).ConfigureAwait(false);

            if (generateAudioFilters)
            {
                var listAudioTracks = await GetAudioTracks(containerUri).ConfigureAwait(false);

                // Once we have the list of audio tracks, we want to order them by their track index, then create filter names starting at 0:
                int filterIndex = 0;
                foreach (var audioTrack in listAudioTracks.OrderBy(t => t.TrackID))
                {
                    string filterName = string.Format(CultureInfo.InvariantCulture, "audio{0:00}", filterIndex++);
                    await CreateTrackFilterAsync(assetName, filterName, new List<TrackInfo>() { audioTrack }).ConfigureAwait(false);
                }

                // In the future, we might want to create a filter for each audio with the video:
                // filterIndex = 0;
                // var videoTrack = new TrackInfo() { TrackID = 1, TrackName = "video", TrackType = "Video" };
                // foreach (var audioTrack in listAudioTracks.OrderBy(t => t.TrackID))
                // {
                //     string filterName = string.Format(CultureInfo.InvariantCulture, "video00audio{0:00}", filterIndex++);
                //     await CreateTrackFilterAsync(
                //         assetName,
                //         filterName,
                //         new List<TrackInfo>()
                //         {
                //             audioTrack,
                //             videoTrack
                //         }).ConfigureAwait(false);
                // }
            }

            string defaultContentKeyPolicyName = await GetDefaultContentKeyPolicyNameAsync(contentKeyPolicyName).ConfigureAwait(false);

            string locatorName = "locator-" + uniqueness;
            StreamingLocator locatorCreated = await CreateLocatorAsync(
                locatorName,
                assetName,
                listFilters,
                streamingPolicy,
                defaultContentKeyPolicyName).ConfigureAwait(false);

            StreamingEndpoint selectedStreamingEndpoint = await GetSelectedStreamingEndpointAsync().ConfigureAwait(false);

            ListPathsResponse pathResponse = await GetListPathResponseAsync(locatorName).ConfigureAwait(false);

            Uri dashUri = GetStreamingUri(pathResponse, selectedStreamingEndpoint, StreamingPolicyStreamingProtocol.Dash);
            Uri hlsUri = GetStreamingUri(pathResponse, selectedStreamingEndpoint, StreamingPolicyStreamingProtocol.Hls);

            // Getting the keys id
            var cencKey = locatorCreated.ContentKeys.Where(k => k.Type == StreamingLocatorContentKeyType.CommonEncryptionCenc).FirstOrDefault();
            var cbcsKey = locatorCreated.ContentKeys.Where(k => k.Type == StreamingLocatorContentKeyType.CommonEncryptionCbcs).FirstOrDefault();
            string cencKeyId = cencKey?.Id.ToString();
            string cbcsKeyId = cbcsKey?.Id.ToString();

            return new ServiceOperationResultMediaServicesV3LocatorCreate(
                locatorName,
                cencKeyId,
                cbcsKeyId,
                dashUri,
                hlsUri,
                operationContext);
        }

        private async Task<List<TrackInfo>> GetAudioTracks(Uri containerUri)
        {
            // Used to correlate storage service calls that Braniac doesn't need to know about.
            var internalTracker = new JObject
            {
                { "~AMS-V3-Audio-Track", $"G:{Guid.NewGuid()}" }
            };
            var internalContext = new StorageClientProviderContext(internalTracker, muted: true);

            var blobs = await _storageService.ListBlobsAsync(containerUri, internalContext).ConfigureAwait(false);
            var ismFile = blobs.Where(b => b.Name.EndsWith(".ism", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (ismFile is null)
            {
                var msg = $"Attempt to get audio tracks for container {containerUri} but this container doesn't contain an ism manifest file.";
                throw new GridwichPublicationMissingManifestFileException(
                    containerUri,
                    msg);
            }
            var ismUri = new BlobUriBuilder(containerUri) { BlobName = ismFile.Name }.ToUri();
            var ismStream = await _storageService.DownloadHttpRangeAsync(ismUri, internalContext).ConfigureAwait(false);

            var audioTracks = new List<TrackInfo>();
            using (var sr = new StreamReader(ismStream.Content))
            {
                using var reader = XmlReader.Create(sr);
                var serializer = new XmlSerializer(typeof(IsmFile));
                var manifest = (IsmFile)serializer.Deserialize(reader);
                foreach (var audio in manifest?.Body?.Switch?.Audio)
                {
                    var trackId = audio.Param.Where(p => p.Name.Equals("trackID", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().Value;
                    var trackName = audio.Param.Where(p => p.Name.Equals("trackName", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().Value;
                    audioTracks.Add(new TrackInfo()
                    {
                        TrackID = int.Parse(trackId, NumberFormatInfo.InvariantInfo),
                        TrackName = trackName,
                        TrackType = "Audio"
                    });
                }
            }

            return audioTracks;
        }

        /// <inheritdoc/>
        public async Task<ServiceOperationResultMediaServicesV3LocatorDelete> LocatorDeleteAsync(
            string locatorName,
            JObject operationContext)
        {
            await ConnectAsync().ConfigureAwait(false);

            try
            {
                await this.MediaServicesV3SdkWrapper.StreamingLocatorDeleteAsync(
                    locatorName)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = "AMS v3 Publish. Error when deleting locator.";
                throw new GridwichPublicationLocatorDeletionException(
                    locatorName,
                    msg,
                    ex);
            }

            return new ServiceOperationResultMediaServicesV3LocatorDelete(
                locatorName,
                operationContext);
        }

        /// <summary>
        /// Gets (or updates or creates) the default ContentKeyPolicy name for the requested protection.
        /// </summary>
        /// <param name="contentKeyPolicyName">The type of requested protection.</param>
        /// <returns>The DefaultContentKeyPolicyName.</returns>
        /// <exception cref="GridwichPublicationContentKeyPolicyNotSupportedException">The Content key policy named contentKeyPolicyName could not be located in the list of valid content key policies in Media Services V3.</exception>
        /// <exception cref="GridwichPublicationContentKeyPolicyException">AMS v3 Publish. Error when creating/updating the content key policy.</exception>
        private async Task<string> GetDefaultContentKeyPolicyNameAsync(string contentKeyPolicyName)
        {
            if (string.IsNullOrWhiteSpace(contentKeyPolicyName))
            {
                // If no policy name is passed, then we assume none / clear.
                return null;
            }

            // Attempt to find the content key policy in the Media Services V3 Dictionary
            var mediaServicesV3ContentKeyPolicyServicePolicy = _mediaServicesV3ContentKeyPolicyService.GetContentKeyPolicyFromMemory(contentKeyPolicyName);
            // Check to see if the content key policy was located
            if (mediaServicesV3ContentKeyPolicyServicePolicy == null)
            {
                var msg = $"The Content key policy named {contentKeyPolicyName} could not be located in the list of valid content key policies in Media Services V3.";
                throw new GridwichPublicationContentKeyPolicyNotSupportedException(
                    contentKeyPolicyName,
                    msg);
            }
            else
            {
                // to get the right casing (optional but cleaner)
                contentKeyPolicyName = mediaServicesV3ContentKeyPolicyServicePolicy.Name;
            }


            // let's try to find the policy in AMS
            ContentKeyPolicy contentKeyPolicy;
            bool createStreamingPolicy = false;
            try
            {
                contentKeyPolicy = await this.MediaServicesV3SdkWrapper.ContentKeyPolicyGetAsync(contentKeyPolicyName).ConfigureAwait(false);
            }
            catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                createStreamingPolicy = true;
            }
            catch (Exception ex)
            {
                var msg = AddDetailedAMSMessage(ex, "AMS v3 Publish. Error when getting the content key policy.");
                throw new GridwichPublicationContentKeyPolicyException(
                    contentKeyPolicyName,
                    msg,
                    ex);
            }


            // If the content key policy does not exist, or if it has not been updated in the current Azure function session, let's create / update it.
            if ((!mediaServicesV3ContentKeyPolicyServicePolicy.CreatedOrUpdatedDone && enableContentKeyPolicyAutomaticUpdate) || createStreamingPolicy)
            {
                // Create or update the content key policy
                // This is done each time the first locator is done in the session (as DRM secrets may have been changed).
                try
                {
                    // Asynchronously wait to enter the Semaphore. We want to avoid two updates of the policy at the same time.
                    await SemaphoreUpdateContentKeyPolicy.WaitAsync().ConfigureAwait(false);

                    // Pattern : Double-checked locking
                    createStreamingPolicy = false;
                    try
                    {
                        contentKeyPolicy = await this.MediaServicesV3SdkWrapper
                            .ContentKeyPolicyGetAsync(contentKeyPolicyName).ConfigureAwait(false);
                    }
                    catch (ErrorResponseException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        createStreamingPolicy = true;
                    }
                    catch (Exception ex)
                    {
                        var msg = AddDetailedAMSMessage(ex, "AMS v3 Publish. Error when getting the content key policy.");
                        throw new GridwichPublicationContentKeyPolicyException(
                            contentKeyPolicyName,
                            msg,
                            ex);
                    }

                    // Pattern : Double-checked locking
                    if ((!mediaServicesV3ContentKeyPolicyServicePolicy.CreatedOrUpdatedDone && enableContentKeyPolicyAutomaticUpdate) || createStreamingPolicy)
                    {
                        try
                        {
                            await this.MediaServicesV3SdkWrapper
                                .ContentKeyPolicyCreateOrUpdateAsync(contentKeyPolicyName,
                                mediaServicesV3ContentKeyPolicyServicePolicy.ContentKeyPolicyOptions)
                                .ConfigureAwait(false);
                            mediaServicesV3ContentKeyPolicyServicePolicy.CreatedOrUpdatedDone = true;
                        }
                        catch (ErrorResponseException ex)
                        {
                            // if there is a conflict... But this should not happen thanks to the Semaphore.
                            if (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict &&
                                createStreamingPolicy)
                            {
                                EventId id = LogEventIds.MediaServicesV3ContentKeyPolicyConflict;
                                Log.LogEvent(id, "Conflict when updating the content key policy", contentKeyPolicyName);
                            }
                            else
                            {
                                var msg = AddDetailedAMSMessage(ex, "AMS v3 Publish. Error when creating/updating the content key policy.");
                                throw new GridwichPublicationContentKeyPolicyException(
                                    contentKeyPolicyName,
                                    msg,
                                    ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var msg = AddDetailedAMSMessage(ex, "AMS v3 Publish. Error in creating/updating the content key policy.");
                    throw new GridwichPublicationContentKeyPolicyException(
                        contentKeyPolicyName,
                        msg,
                        ex);
                }
                finally
                {
                    SemaphoreUpdateContentKeyPolicy.Release();
                }
            }
            return contentKeyPolicyName;
        }

        /// <summary>
        /// Add the AMS message to text of the exception
        /// </summary>
        /// <param name="e">exception</param>
        /// <param name="message">message from Gridwich</param>
        /// <returns>extended message</returns>
        private static string AddDetailedAMSMessage(Exception e, string message)
        {
            if (e != null && e is ErrorResponseException eApi)
            {
                {
                    dynamic error = JsonConvert.DeserializeObject(eApi.Response.Content);
                    message += " Reason : " + (string)error?.error?.message;
                }
            }

            return message;
        }
    }
}