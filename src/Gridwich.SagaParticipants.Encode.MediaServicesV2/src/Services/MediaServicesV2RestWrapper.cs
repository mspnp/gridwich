using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;
using RestSharp.Serializers.NewtonsoftJson;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Services
{
    /// <summary>
    /// Azure Media Service client provider.
    ///     /// Ref:
    ///   https://docs.microsoft.com/en-us/rest/api/media/operations/azure-media-services-rest-api-reference
    ///   https://docs.microsoft.com/en-us/azure/media-services/previous/media-services-rest-connect-with-aad
    /// </summary>
    public class MediaServicesV2RestWrapper : IMediaServicesV2RestWrapper
    {
        private readonly IObjectLogger<MediaServicesV2RestWrapper> _log;
        private readonly ISettingsProvider _settingsProvider;
        private readonly TokenCredential _tokenCredential;
        private readonly IRestClient _restClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV2RestWrapper"/> class.
        /// </summary>
        /// <param name="log">log.</param>
        /// <param name="settingsProvider">settingsProvider.</param>
        /// <param name="tokenCredential">TokenCredential.</param>
        public MediaServicesV2RestWrapper(
            IObjectLogger<MediaServicesV2RestWrapper> log,
            ISettingsProvider settingsProvider,
            TokenCredential tokenCredential)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _tokenCredential = tokenCredential;

            var amsAccountName = _settingsProvider.GetAppSettingsValue("AmsAccountName");
            var amsLocation = _settingsProvider.GetAppSettingsValue("AmsLocation");
            var baseUrl = $"https://{amsAccountName}.restv2.{amsLocation}.media.azure.net/api/";
            _restClient = new RestClient(baseUrl);
        }

        private readonly object configLock = new object();
        private bool isConfigured = false;
        private void ConfigureRestClient()
        {
            if (!isConfigured)
            {
                Exception exceptionInLock = null;
                lock (configLock)
                {
                    if (!isConfigured)
                    {
                        try
                        {
                            var amsAccessToken = _tokenCredential.GetToken(
                                new TokenRequestContext(
                                    scopes: new[] { "https://rest.media.azure.net/.default" },
                                    parentRequestId: null),
                                default);

                            _restClient.Authenticator = new JwtAuthenticator(amsAccessToken.Token);

                            _restClient.UseNewtonsoftJson(
                                new Newtonsoft.Json.JsonSerializerSettings()
                                {
                                    ContractResolver = new DefaultContractResolver(),
                                });

                            _restClient.AddDefaultHeader("Content-Type", $"{ContentType.Json};odata=verbose");
                            _restClient.AddDefaultHeader("Accept", $"{ContentType.Json};odata=verbose");
                            _restClient.AddDefaultHeader("DataServiceVersion", "3.0");
                            _restClient.AddDefaultHeader("MaxDataServiceVersion", "3.0");
                            _restClient.AddDefaultHeader("x-ms-version", "2.19");
                        }
                        catch (Exception e)
                        {
                            exceptionInLock = e;
                            _log.LogException(LogEventIds.MediaServicesV2ConnectionError, e, $"Failed to {nameof(ConfigureRestClient)}");
                        }
                        finally
                        {
                            isConfigured = true;
                        }
                    }
                }
                if (exceptionInLock != null)
                {
                    throw exceptionInLock;
                }
            }
            return;
        }

        /// <inheritdoc/>
        public async Task<string> GetOrCreateNotificationEndPointAsync(string notificationEndPointName, Uri callbackEndpoint)
        {
            ConfigureRestClient();

            // Try to get existing notification endpoint:
            var getRequest = new RestRequest($"NotificationEndPoints", Method.GET);
            var getResponse = await _restClient.ExecuteAsync<JObject>(getRequest, cancellationToken: default).ConfigureAwait(false);
            if (getResponse.IsSuccessful)
            {
                var existingNotificationEndPoint = getResponse.Data["d"]["results"]?.Where(r =>
                    ((string)r["Name"])?.ToUpperInvariant() == notificationEndPointName.ToUpperInvariant() &&
                    ((string)r["EndPointAddress"])?.ToUpperInvariant() == callbackEndpoint.ToString().ToUpperInvariant()).FirstOrDefault();

                var existingNotificationEndPointId = (string)existingNotificationEndPoint?.SelectToken("Id", false);
                if (!string.IsNullOrWhiteSpace(existingNotificationEndPointId))
                {
                    // We have a match, no need to create one.
                    return existingNotificationEndPointId;
                }
            }

            // Create a new notification endpoint:
            var request = new RestRequest($"NotificationEndPoints", Method.POST);

            JObject jsonBody = new JObject()
            {
                new JProperty("Name", notificationEndPointName),
                new JProperty("EndPointAddress", callbackEndpoint),
                new JProperty("EndPointType", 3),
                new JProperty("CredentialType", 0),
                // EncryptedEndPointCredential = null,
                // ProtectionKeyId = null,
                new JProperty("ProtectionKeyType", 0),
            };
            request.AddJsonBody(jsonBody);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            return (string)restResponse.Data.SelectToken("d.Id", true);
        }

        /// <inheritdoc/>
        public async Task<(string AssetId, Uri AssetUri)> CreateEmptyAssetAsync(string assetName, string accountName)
        {
            ConfigureRestClient();

            var request = new RestRequest($"Assets", Method.POST);
            JObject jsonBody = new JObject()
            {
                new JProperty("Name", assetName),
                new JProperty("StorageAccountName", accountName),
            };
            request.AddJsonBody(jsonBody);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.:  {"d":{"__metadata":{"id":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')","uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')","type":"Microsoft.Cloud.Media.Vod.Rest.Data.Models.Asset","actions":{"https://gridwichamssb.restv2.westus.media.azure.net/api/$metadata#WindowsAzureMediaServices.Publish":[{"title":"Publish","target":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/Publish"}]}},"Locators":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/Locators"}},"ContentKeys":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/ContentKeys"}},"Files":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/Files"}},"ParentAssets":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/ParentAssets"}},"DeliveryPolicies":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/DeliveryPolicies"}},"AssetFilters":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/AssetFilters"}},"StorageAccount":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A7e712f7e-d35f-4629-a9b1-47b35b2b576b')/StorageAccount"}},"Id":"nb:cid:UUID:7e712f7e-d35f-4629-a9b1-47b35b2b576b","State":0,"Created":"\/Date(1587084991763)\/","LastModified":"\/Date(1587084991763)\/","AlternateId":null,"Name":"V2-gridwichinbox00sasb-unikittyclipsmall-Input","Options":0,"FormatOption":0,"Uri":"https://gridwichinbox00sasb.blob.core.windows.net/asset-7e712f7e-d35f-4629-a9b1-47b35b2b576b","StorageAccountName":"gridwichinbox00sasb"}}
            if (!restResponse.IsSuccessful)
            {
                // Bad payload format: {"error":{"code":"","message":{"lang":"en-US","value":"Parsing request content failed due to: Make sure to only use property names that are defined by the type"}}}
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var assetId = (string)restResponse.Data.SelectToken("d.Id", true);
            var assetUri = (Uri)restResponse.Data.SelectToken("d.Uri", true);
            return (assetId, assetUri);
        }

        /// <inheritdoc/>
        public async Task<(string AssetName, Uri AssetUri)> GetAssetNameAndUriAsync(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Assets('{assetId}')", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.: similar to CreateEmptyAssetAsync()
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var assetName = (string)restResponse.Data.SelectToken("d.Name", true);
            var assetUri = (Uri)restResponse.Data.SelectToken("d.Uri", true);
            return (assetName, assetUri);
        }


        /// <inheritdoc/>
        public async Task CreateFileInfosAsync(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"CreateFileInfos", Method.GET);
            request.AddParameter("assetid", $"'{assetId}'");
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.: status 204, no content.

            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            // Note:
            //  CreateFileInfos asks Azure Media Services to enumerate files in the asset container and add them as AssetFiles,
            //  without auto-selecting a primary file.  A primary file is needed for encodes that need to diambiguate which file
            //  should be used as the master timeline, or primary video track, etc.  This code base is not expected to need
            //  primary files, so does not attempt to itterate over the created AssetFiles to then set one as Primary.
            //  Should this be needed in the future, the following operations should be considered:
            //  new RestRequest($"Assets('{assetId}')/Files", Method.GET);
            //  foreach(var assetFile in restResponse.Data["d"]["results"].ToList())
            //      var uriString = (string)assetFile.SelectToken("Uri", true);
            //      Use some selection criteria to find the file that you want as primary.
            //      var assetFileIdToUseAsPrimary = (string)assetFile.SelectToken("Id", true); break;
            //  new RestRequest($"Files('{assetFileIdToUseAsPrimary}')", Method.MERGE);
            //  request.AddJsonBody(new {IsPrimary = true});
            //  Ref: https://docs.microsoft.com/en-us/rest/api/media/operations/assetfile#Update_a_file
        }

        /// <inheritdoc/>
        public Task<string> GetLatestMediaProcessorAsync(string mediaProcessorName)
        {
            // TODO: Will this ever change?
            return Task.FromResult("nb:mpid:UUID:ff4df607-d419-42f0-bc17-a481b1331e56");
        }

        /// <inheritdoc/>
        public async Task<string> CreateJobAsync(string jobName, string processorId, string inputAssetId, string preset, string outputAssetName, string outputAssetStorageAccountName, string correlationData, string notificationEndPointId)
        {
            // Ref:
            //     https://docs.microsoft.com/en-us/rest/api/media/operations/job#create_jobs_with_notifications
            //     https://social.msdn.microsoft.com/Forums/en-US/cc69a85f-74b0-4d52-8e69-629ff5007169/create-an-encoding-job-with-jobnotificationsubscriptions-by-using-rest-api-got-a-response-with-400
            // In this implementation we use a single POST, versus the documented /$batch method, by forcing the header to "application/json" and using "InputMediaAssets@odata.bind" instead of "InputMediaAssets".

            ConfigureRestClient();

            var request = new RestRequest($"Jobs", Method.POST);
            // The use of '@odata.bind' in the body means that we should not use 'odata=verbose'
            request.AddHeader("Content-Type", ContentType.Json);
            JObject jsonBody = new JObject()
            {
                new JProperty("Name", jobName),

                new JProperty("InputMediaAssets@odata.bind",
                    new JArray(
                        $"{_restClient.BaseUrl}Assets('{inputAssetId}')")),

                new JProperty("Tasks",
                    new JArray(
                        new JObject()
                        {
                            new JProperty("Name", correlationData),
                            new JProperty("Configuration", preset),
                            new JProperty("MediaProcessorId", processorId),
                            new JProperty("TaskBody", $"<?xml version=\"1.0\" encoding=\"utf-8\"?><taskBody><inputAsset>JobInputAsset(0)</inputAsset><outputAsset assetName=\"{outputAssetName}\" storageAccountName=\"{outputAssetStorageAccountName}\" assetCreationOptions=\"0\" assetFormatOption=\"0\" >JobOutputAsset(0)</outputAsset></taskBody>"),
                            new JProperty("TaskNotificationSubscriptions",
                                new JArray(
                                    new JObject()
                                    {
                                        new JProperty("IncludeTaskProgress", true),
                                        new JProperty("NotificationEndPointId", notificationEndPointId),
                                        new JProperty("TargetTaskState", 2),
                                    }))
                        }))
            };
            request.AddJsonBody(jsonBody);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var jobId = (string)restResponse.Data.SelectToken("d.Id", true);
            return jobId;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetAssetFilesNames(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Assets('{assetId}')/Files", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.: {"d":{"results":[{"__metadata":{"id":"https://gridwichamssb.restv2.westus.media.azure.net/api/Files('nb%3Acid%3AUUID%3A08091296-b368-4444-9ce9-878798ef482e')","uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Files('nb%3Acid%3AUUID%3A08091296-b368-4444-9ce9-878798ef482e')","type":"Microsoft.Cloud.Media.Vod.Rest.Data.Models.AssetFile"},"Id":"nb:cid:UUID:08091296-b368-4444-9ce9-878798ef482e","Name":"Unikitty_Clip_small.mov","ContentFileSize":"122992491","ParentAssetId":"nb:cid:UUID:3682f1d9-05f5-442a-832b-afe25cfec903","EncryptionVersion":null,"EncryptionScheme":null,"IsEncrypted":false,"EncryptionKeyId":null,"InitializationVector":null,"IsPrimary":false,"LastModified":"\/Date(1587097903363)\/","Created":"\/Date(1587097903363)\/","MimeType":null,"ContentChecksum":null,"Options":0}]}}
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            return restResponse.Data["d"]["results"]?.Select(r => (string)r.SelectToken("Name")).ToList();
        }

        /// <inheritdoc/>
        public async Task<(string FirstTaskId, string FirstTaskName)> GetFirstTaskAsync(string jobId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')/Tasks", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.: {"d":{"results":[{"__metadata":{"id":"https://gridwichamssb.restv2.westus.media.azure.net/api/Tasks('nb%3Atid%3AUUID%3A45547fae-f89c-406c-8202-773ce47f4ac8')","uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Tasks('nb%3Atid%3AUUID%3A45547fae-f89c-406c-8202-773ce47f4ac8')","type":"Microsoft.Cloud.Media.Vod.Rest.Data.Models.Task"},"OutputMediaAssets":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Tasks('nb%3Atid%3AUUID%3A45547fae-f89c-406c-8202-773ce47f4ac8')/OutputMediaAssets"}},"InputMediaAssets":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Tasks('nb%3Atid%3AUUID%3A45547fae-f89c-406c-8202-773ce47f4ac8')/InputMediaAssets"}},"Id":"nb:tid:UUID:45547fae-f89c-406c-8202-773ce47f4ac8","Configuration":"\n {\n 'Codecs': [\n {\n 'SyncMode': 'Auto',\n 'JpgLayers': [\n {\n 'Quality': 90,\n 'Type': 'JpgLayer',\n 'Width': '10%',\n 'Height': '10%'\n }\n ],\n 'SpriteColumn': 10,\n 'Start': '00:00:01',\n 'Step': '1%',\n 'Range': '100%',\n 'Type': 'JpgImage'\n }\n ],\n 'Outputs': [\n {\n 'FileName': '{Basename}_{Index}_Sprite{Extension}',\n 'Format': {\n 'Type': 'JpgFormat'\n }\n }\n ],\n 'Version': 1.0\n }\n ","EndTime":"2020-04-15T18:46:11.477Z","ErrorDetails":{"__metadata":{"type":"Collection(Microsoft.Cloud.Media.Vod.Rest.Data.Models.ErrorDetail)"},"results":[]},"HistoricalEvents":{"__metadata":{"type":"Collection(Microsoft.Cloud.Media.Vod.Rest.Data.Models.TaskHistoricalEvent)"},"results":[{"Code":"WaitingForFreeResources","Message":"","TimeStamp":"2020-04-15T18:45:16.37"},{"Code":"Scheduled","Message":"","TimeStamp":"2020-04-15T18:45:22.217"},{"Code":"Processing","Message":"","TimeStamp":"2020-04-15T18:45:24.54"},{"Code":"Finished","Message":"","TimeStamp":"2020-04-15T18:46:11.477"},{"Code":"TaskBillingInformationAdded","Message":"Mediatask 4fdccddf-0e5f-4e52-9990-1855d3c60e23: {\r\n \"NumberOfImages\": 1,\r\n \"SDVideoOutputMinutes\": 0.0,\r\n \"HDVideoOutputMinutes\": 0.0,\r\n \"UltraHDVideoOutputMinutes\": 0.0,\r\n \"AudioOutputMinutes\": 0.0,\r\n \"TotalOutputMinutes\": 0.02,\r\n \"VOD Standard Encoding Output Minutes\": 0.02\r\n}\r\n","TimeStamp":"2020-04-15T18:46:12.453"}]},"MediaProcessorId":"nb:mpid:UUID:ff4df607-d419-42f0-bc17-a481b1331e56","Name":"ew0KICAib3V0cHV0QXNzZXRDb250YWluZXIiOiAiaHR0cHM6Ly9oYXJkYWNzcHJpdGVzMDBzYXNiLmJsb2IuY29yZS53aW5kb3dzLm5ldC9jMGUxZTljZi0zNTBiLTQzNzgtODA2Yi1iMjZmMzhmNDRmYWIiLA0KICAib3BlcmF0aW9uQ29udGV4dCI6ICJ7XHJcbiAgXCJwcm9jZXNzSWRcIjogNTAwNFxyXG59Ig0KfQ","PerfMessage":"Mediatask 4fdccddf-0e5f-4e52-9990-1855d3c60e23: Download: 00:00:15.2505407\nEncode 4fdccddf\nStream Info for file Scoob_16Ch_mono.mov\r\nVideo stream duration is 171.171\r\nVideo stream bitrate is 147477570\r\nTotal video stream duration is 171.171\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nAudio stream duration is 171.171\r\nAudio stream bitrate is 1152000\r\nTotal audio stream duration is 2738.736\r\nEncode Subtask\r\nCPU Usage : 22.20 %\r\nMemory Usage : 1169.891 MB\r\nDisk reads / sec : 3.309\r\nDisk writes / sec : 589.145\r\nDisk transfers / sec : 592.455\r\nNetwork Utilization / sec : 0.04 %\r\nEncode File Scoob_16Ch_mono.mov , size is 3385 Mbytes, Encoding time is 18.3130914, ratio of encoding time over input duration is 0.00668669565208554\nUpload(optimized): 00:00:00.4219127\n\r\n","Priority":0,"Progress":100,"RunningDuration":39605,"StartTime":"2020-04-15T18:45:24.54Z","State":3,"TaskBody":"<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<taskBody>\r\n <inputAsset>JobInputAsset(0)</inputAsset>\r\n <outputAsset assetCreationOptions=\"0\" assetFormatOption=\"0\" assetName=\"V2-gridwichsprites00sasb-c0e1e9cf-350b-4378-806b-b26f38f44fab-Output\" storageAccountName=\"gridwichsprites00sasb\">JobOutputAsset(0)</outputAsset>\r\n</taskBody>","Options":0,"EncryptionKeyId":null,"EncryptionScheme":"None","EncryptionVersion":null,"InitializationVector":null,"TaskNotificationSubscriptions":{"__metadata":{"type":"Collection(Microsoft.Cloud.Media.Vod.Rest.Data.Models.TaskNotificationSubscription)"},"results":[{"IncludeTaskProgress":true,"NotificationEndPointId":"nb:nepid:UUID:12ed9f0d-947b-4852-aef4-7a708b0ab03f","TargetTaskState":2}]}}]}}
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var firstTaskId = (string)restResponse.Data.SelectToken("d.results[0].Id", true);
            var firstTaskName = (string)restResponse.Data.SelectToken("d.results[0].Name", true);
            return (firstTaskId, firstTaskName);
        }

        /// <inheritdoc/>
        public async Task<(string FirstInputAssetId, string FirstInputAssetName)> GetFirstInputAssetAsync(string jobId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')/InputMediaAssets", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.:{"d":{"results":[{"__metadata":{"id":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')","uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')","type":"Microsoft.Cloud.Media.Vod.Rest.Data.Models.Asset","actions":{"https://gridwichamssb.restv2.westus.media.azure.net/api/$metadata#WindowsAzureMediaServices.Publish":[{"title":"Publish","target":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/Publish"}]}},"Locators":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/Locators"}},"ContentKeys":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/ContentKeys"}},"Files":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/Files"}},"ParentAssets":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/ParentAssets"}},"DeliveryPolicies":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/DeliveryPolicies"}},"AssetFilters":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/AssetFilters"}},"StorageAccount":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3A5f15fd01-899e-4b6b-9054-c31bdbaed668')/StorageAccount"}},"Id":"nb:cid:UUID:5f15fd01-899e-4b6b-9054-c31bdbaed668","State":2,"Created":"\/Date(1587100549955)\/","LastModified":"\/Date(1587100549955)\/","AlternateId":null,"Name":"Nonexistent Asset","Options":0,"FormatOption":0,"Uri":null,"StorageAccountName":null}]}}
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var firsInputAssetId = (string)restResponse.Data.SelectToken("d.results[0].Id", true);
            var firstInputAssetName = (string)restResponse.Data.SelectToken("d.results[0].Name", true);
            return (firsInputAssetId, firstInputAssetName);
        }

        /// <inheritdoc/>
        public async Task<(string FirstOutputAssetId, string FirstOutputAssetName)> GetFirstOutputAssetAsync(string jobId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Jobs('{jobId}')/OutputMediaAssets", Method.GET);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.: {"d":{"results":[{"__metadata":{"id":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')","uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')","type":"Microsoft.Cloud.Media.Vod.Rest.Data.Models.Asset","actions":{"https://gridwichamssb.restv2.westus.media.azure.net/api/$metadata#WindowsAzureMediaServices.Publish":[{"title":"Publish","target":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/Publish"}]}},"Locators":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/Locators"}},"ContentKeys":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/ContentKeys"}},"Files":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/Files"}},"ParentAssets":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/ParentAssets"}},"DeliveryPolicies":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/DeliveryPolicies"}},"AssetFilters":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/AssetFilters"}},"StorageAccount":{"__deferred":{"uri":"https://gridwichamssb.restv2.westus.media.azure.net/api/Assets('nb%3Acid%3AUUID%3Ad7071808-a5ac-4a19-82cf-eb4d0f71edd7')/StorageAccount"}},"Id":"nb:cid:UUID:d7071808-a5ac-4a19-82cf-eb4d0f71edd7","State":2,"Created":"\/Date(1587100642316)\/","LastModified":"\/Date(1587100642316)\/","AlternateId":null,"Name":"Nonexistent Asset","Options":0,"FormatOption":0,"Uri":null,"StorageAccountName":null}]}}
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }

            var firsOutputAssetId = (string)restResponse.Data.SelectToken("d.results[0].Id", true);
            var firstOutputAssetName = (string)restResponse.Data.SelectToken("d.results[0].Name", true);
            return (firsOutputAssetId, firstOutputAssetName);
        }

        /// <inheritdoc/>
        public async Task DeleteAssetAsync(string assetId)
        {
            ConfigureRestClient();
            var request = new RestRequest($"Assets('{assetId}')", Method.DELETE);
            var restResponse = await _restClient.ExecuteAsync<JObject>(request, cancellationToken: default).ConfigureAwait(false);
            // e.g.: status 204, no content.
            if (!restResponse.IsSuccessful)
            {
                string expMsg = CreateExceptionMessage(restResponse);
                throw new Exception(expMsg);
            }
        }

        private string CreateExceptionMessage(IRestResponse<JObject> restResponse)
        {
            var responseHeaders = restResponse.Headers?.Select(h => new JProperty(h.Name, (string)h.Value));
            JObject requestBody = restResponse.Request.Body != null && restResponse.Request.Body.ContentType.Contains(ContentType.Json) ? JObject.Parse((string)restResponse.Request.Body.Value) : null;
            var msg = new JObject
                {
                    { "method", restResponse.Request.Method.ToString() },
                    { "host", _restClient.BaseUrl.ToString() },
                    { "path", restResponse.Request.Resource },
                    { "requestBody", requestBody },
                    { "responseUri", restResponse.ResponseUri },
                    { "responseHeaders",  new JObject(responseHeaders) },
                    { "responseStatusCode", restResponse.StatusCode.ToString() },
                    { "responseBody", restResponse.Data },
                };
            return msg.ToString();
        }
    }
}
