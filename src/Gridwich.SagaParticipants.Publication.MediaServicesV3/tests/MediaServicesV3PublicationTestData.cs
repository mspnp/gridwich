using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3Tests
{
    [ExcludeFromCodeCoverage]
    public static class MediaServicesV3PublicationTestData
    {
        // Good Reference Data

        private const string AttachedStorage = "attachedstorage";
        private const string NotAttachedStorage = "notattachedstorage";

        public const string DefaultStorageId = "/subscriptions/28a75405-95db-4d15-9a7f-ab84003a63aa/resourceGroups/xpouyatdemo/providers/microsoft.storage/storageAccounts/" + AttachedStorage;
        public static Uri GoodContainerUri => new Uri("https://" + AttachedStorage + ".blob.core.windows.net/containerasset1/");
        public static Uri BadContainerUriNotAttachedStorage => new Uri("https://" + NotAttachedStorage + ".blob.core.windows.net/containerasset1/");

        public static string GoodContainerName => "28a75405-95db-4d15-9a7f-ab84003a63aa";
        public static string GoodAssetName => $"{AttachedStorage}-{GoodContainerName}";

        public static string GoodClearStreamingPolicy => "clearStreamingOnly";
        public static string GoodDRMStreamingPolicy => "multiDrmStreaming";
        public static string GoodDRMCencStreamingPolicy => "cencDrmStreaming";

        public static string GoodContentKeyPolicyDrm => "multiDrmKey";
        public static string GoodContentKeyPolicyDrmCenc => "cencDrmKey";

        public static string EmptyContentKeyPolicy => string.Empty;

        public static string GoodLocatorName => "loce63fa3bdaa";
        public static string GoodCENCKeyId => "28a75405-0000-0000-0000-ab84003a63aa";
        public static string GoodCBCSKeyId => "28a75405-0000-0000-0001-ab84003a63aa";
        public static Uri GoodDashUri => new Uri("https://gridwichamssb-uswe.streaming.media.azure.net//85868ec8-13ac-436e-955f-066c7178a5ce/Angus_NTSC_4x3_2ch_noCC_i.ism/manifest(format=mpd-time-csf)");
        public static Uri GoodHlsUri => new Uri("https://gridwichamssb-uswe.streaming.media.azure.net//85868ec8-13ac-436e-955f-066c7178a5ce/Angus_NTSC_4x3_2ch_noCC_i.ism/manifest(format=m3u8-aapl)");

        public static string AmsDrmFairPlayAskHex => "8D49FD8E59E19F69A52F0E9E0D5742F8";
        public static string AmsDrmFairPlayPfxPassword => "Password!2020";

        // NOTE: *.pfx is in .gitignore, if you rename folders this test file may not move correctly, or it may be ignored at it's new location.
        // This will cause this class to fail to instantiate.
        private const string LocalFairPlayCert = "FakeFairPlayCert/FairPlay-out-pfx.bin";

        private static readonly string PathCert = TestHelpers.GetPathRelativeToTests(LocalFairPlayCert);
        public static X509Certificate2 AmsDrmFairPlayCertificate => new X509Certificate2(PathCert, AmsDrmFairPlayPfxPassword, X509KeyStorageFlags.Exportable);
        public static string AmsDrmFairPlayCertAsSecretString => Convert.ToBase64String(AmsDrmFairPlayCertificate.Export(X509ContentType.Pfx, AmsDrmFairPlayPfxPassword));
        // static value that does not change if used everal times
        public static readonly string AmsDrmFairPlayCertAsSecretStringStatic = AmsDrmFairPlayCertAsSecretString;
        public static readonly string AmsDrmFairPlayPfx = Convert.ToBase64String(AmsDrmFairPlayCertificate.Export(X509ContentType.Pfx, AmsDrmFairPlayPfxPassword));

        public static string AmsDrmOpenIdConnectDiscoveryDocument => "https://domain.com/.well-known/OpenIdConfiguration";

        public static string Clear => "clear";
        public static JObject GoodOperationContext => new JObject()
        {
            new JProperty("expectedKey", "expectedValue"),
            new JProperty("expectedId", 42),
        };

        public static List<BlobItem> GoodBlobItemListWithIsmFile => new List<BlobItem>() { BlobsModelFactory.BlobItem("somefile.ism", false, null) };

        public static BlobDownloadInfo GoodIsmFileBlobDownloadInfo => BlobsModelFactory.BlobDownloadInfo(
            content: new MemoryStream(new UTF8Encoding()
                .GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><smil xmlns=\"http://www.w3.org/2001/SMIL20/Language\"><body><switch><audio src=\"UNIKITTY_YR02_18_19_(SEG)_SCARY__320x180_AACAudio_257.mp4\" systemBitrate=\"127999\" systemLanguage=\"eng\"><param name=\"systemBitrate\" value=\"127999\" valuetype=\"data\" /><param name=\"trackID\" value=\"2\" valuetype=\"data\" /><param name=\"trackName\" value=\"aac_eng_2_127999_2_1\" valuetype=\"data\" /><param name=\"systemLanguage\" value=\"eng\" valuetype=\"data\" /><param name=\"trackIndex\" value=\"UNIKITTY_YR02_18_19_(SEG)_SCARY__320x180_AACAudio_257_2.mpi\" valuetype=\"data\" /></audio><audio src=\"UNIKITTY_YR02_18_19_(SEG)_SCARY__320x180_AACAudio_257.mp4\" systemBitrate=\"127999\" systemLanguage=\"eng\"><param name=\"systemBitrate\" value=\"127999\" valuetype=\"data\" /><param name=\"trackID\" value=\"3\" valuetype=\"data\" /><param name=\"trackName\" value=\"aac_eng_2_127999_3_1\" valuetype=\"data\" /><param name=\"systemLanguage\" value=\"eng\" valuetype=\"data\" /><param name=\"trackIndex\" value=\"UNIKITTY_YR02_18_19_(SEG)_SCARY__320x180_AACAudio_257_3.mpi\" valuetype=\"data\" /></audio><video src=\"UNIKITTY_YR02_18_19_(SEG)_SCARY__320x180_AACAudio_257.mp4\" systemBitrate=\"257281\"><param name=\"systemBitrate\" value=\"257281\" valuetype=\"data\" /><param name=\"trackID\" value=\"1\" valuetype=\"data\" /><param name=\"trackName\" value=\"video\" valuetype=\"data\" /><param name=\"trackIndex\" value=\"UNIKITTY_YR02_18_19_(SEG)_SCARY__320x180_AACAudio_257_1.mpi\" valuetype=\"data\" /></video></switch></body></smil>")));

        // Outputs:

        public static ServiceOperationResultMediaServicesV3LocatorCreate ServiceOperationResult_Is_Expected => new ServiceOperationResultMediaServicesV3LocatorCreate(
            locatorName: GoodLocatorName,
            cencKeyId: GoodCENCKeyId,
            cbcsKeyId: GoodCBCSKeyId,
            dashUri: GoodDashUri,
            hlsUri: GoodHlsUri,
            operationContext: GoodOperationContext);

        // Inputs:

        public static RequestMediaServicesLocatorCreateDTO RequestMediaServicesLocatorCreateDTO_Is_Expected => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = 10.0,
                EndSeconds = 22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoWithoutFilter => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoWithFilterStart => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = 10.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoWithFilterEnd => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                EndSeconds = 22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoWithGenerateAudioFilters => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            OperationContext = GoodOperationContext,
            GenerateAudioFilters = true
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoClear => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoDRM => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodDRMStreamingPolicy,
            ContentKeyPolicyName = GoodContentKeyPolicyDrm,
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO GoodDataDtoDRMCenc => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodDRMCencStreamingPolicy,
            ContentKeyPolicyName = GoodContentKeyPolicyDrmCenc,
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataDtoFilter => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = -10.0,
                EndSeconds = -22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataDtoFilterWhichThrowException => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = (12 * 60) + 12.0,
                EndSeconds = (13 * 60) + 13.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataDtoAssetFilterWhichThrowException => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            OperationContext = GoodOperationContext,
            GenerateAudioFilters = true
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataDtoNoContainer => new RequestMediaServicesLocatorCreateDTO()
        {
            StreamingPolicyName = GoodClearStreamingPolicy,
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = 10.0,
                EndSeconds = 22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataStreamingPolicyNotSupported => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = "anotherpolicynotsupported",
            ContentKeyPolicyName = EmptyContentKeyPolicy,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = 10.0,
                EndSeconds = 22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataContentKeyPolicyNotSupported => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = GoodContainerUri,
            StreamingPolicyName = GoodDRMStreamingPolicy,
            ContentKeyPolicyName = "anotherpolicynotsupported",
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = 10.0,
                EndSeconds = 22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestMediaServicesLocatorCreateDTO BadDataDtoStorageNotAttached => new RequestMediaServicesLocatorCreateDTO()
        {
            ContainerUri = BadContainerUriNotAttachedStorage,
            StreamingPolicyName = GoodDRMStreamingPolicy,
            ContentKeyPolicyName = GoodContentKeyPolicyDrm,
            TimeBasedFilter = new TimeBasedFilterDTO()
            {
                StartSeconds = 10.0,
                EndSeconds = 22.0,
            },
            OperationContext = GoodOperationContext,
        };

        public static RequestBaseDTO Dto_Is_Not_RequestorMediaServicesLocatorCreateDTO => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = "https://gridwichtestin01sasb.blob.core.windows.net/input1/bbb.mp4" },
            },
            OutputContainer = "https://gridwichtestout01sasb.blob.core.windows.net/output1/",
            TransformName = "AdaptiveStreaming",
            OperationContext = GoodOperationContext,
        };
    }
}