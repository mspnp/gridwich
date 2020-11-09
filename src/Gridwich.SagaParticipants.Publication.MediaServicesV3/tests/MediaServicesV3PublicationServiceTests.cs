using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.KeyPolicies;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.StreamingPolicies;
using Gridwich.SagaParticipants.Publication.MediaServicesV3Tests.MockHelper;
using Gridwich.Services.Core.Exceptions;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3Tests
{
    /// <summary>
    /// Tests for the Media Services V3 Service class implementation.
    /// </summary>
    [ExcludeFromCodeCoverage]

    [TestClass]
    public class MediaServicesV3PublicationServiceTests
    {
        private static readonly IObjectLogger<MediaServicesV3BaseService> Log = Mock.Of<IObjectLogger<MediaServicesV3BaseService>>();
        private static readonly IObjectLogger<MediaServicesV3ContentKeyPolicyService> LogKey = Mock.Of<IObjectLogger<MediaServicesV3ContentKeyPolicyService>>();

        private static readonly ListPathsResponse Paths = new ListPathsResponse(new List<StreamingPath>
            {
                new StreamingPath(StreamingPolicyStreamingProtocol.Dash, EncryptionScheme.CommonEncryptionCenc, new List<string>() { "https://test.azure.net/ffhg/test.ism/manifest(format=mpd-time-csf)" }),
                new StreamingPath(StreamingPolicyStreamingProtocol.Hls, EncryptionScheme.CommonEncryptionCbcs, new List<string>() { "https://test.azure.net/ffhg/test.ism/manifest(format=m3u8)" })
            });

        private static readonly StreamingEndpoint StreamingEndpoint1 = new StreamingEndpoint(name: "default", hostName: "test.azure.net", scaleUnits: 0);
        private static readonly StreamingEndpoint StreamingEndpoint2 = new StreamingEndpoint(name: "customname", hostName: "testcustom.azure.net", scaleUnits: 1);

        private static readonly MockPageCollection<StreamingEndpoint> MockPageSe = new MockPageCollection<StreamingEndpoint>(new List<StreamingEndpoint>() { StreamingEndpoint1 }.ToList());
        private static readonly MockPageCollection<StreamingEndpoint> MockPageSe2 = new MockPageCollection<StreamingEndpoint>(new List<StreamingEndpoint>() { StreamingEndpoint1, StreamingEndpoint2 }.ToList());

        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsData
        {
            get
            {
                return new[]
                {
                    new object[] { MediaServicesV3PublicationTestData.RequestMediaServicesLocatorCreateDTO_Is_Expected, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoWithoutFilter, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoWithFilterStart, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoWithFilterEnd, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRM, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRMCenc, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoWithGenerateAudioFilters, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoClear, true, null, EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoClear, false, typeof(GridwichPublicationStreamingPolicyNotSupportedException), EnvironmentTypeConstants.EnvironmentTypeProduction },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoClear, false, typeof(GridwichPublicationStreamingPolicyNotSupportedException), string.Empty },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoClear, false, typeof(GridwichPublicationStreamingPolicyNotSupportedException), null },
                    new object[] { MediaServicesV3PublicationTestData.BadDataDtoNoContainer, false, typeof(ArgumentNullException), EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.BadDataStreamingPolicyNotSupported, false, typeof(GridwichPublicationStreamingPolicyNotSupportedException), EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.BadDataContentKeyPolicyNotSupported, false, typeof(GridwichPublicationContentKeyPolicyNotSupportedException), EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.BadDataDtoStorageNotAttached, false, typeof(GridwichPublicationCreateUpdateAssetException), EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                    new object[] { MediaServicesV3PublicationTestData.BadDataDtoFilter, false, typeof(GridwichTimeParameterException), EnvironmentTypeConstants.EnvironmentTypeDevelopment },
                };
            }
        }

        /// <summary>
        /// Methods used by the tests to know if a filter contains a TimeSpan which should throw an exception.
        /// </summary>
        /// <returns>An asset filter.</returns>
        public static AssetFilter TimingIsWrong()
        {
            return Match.Create<AssetFilter>(s => s.PresentationTimeRange != null && s.PresentationTimeRange.StartTimestamp != null && s.PresentationTimeRange.StartTimestamp == TimeSpan.FromSeconds(MediaServicesV3PublicationTestData.BadDataDtoFilterWhichThrowException.TimeBasedFilter.StartSeconds).Ticks);
        }

        /// <summary>
        /// Testing the Media Services V3 Service Create Locator feature.
        /// </summary>
        /// <param name="locatorCreateDTO">RequestorMediaServicesLocatorCreateDTO data.</param>
        /// <param name="expectedValue">Expected result.</param>
        /// <param name="typeException">Expected type of Exception.</param>
        /// <param name="environmentType">Expected type of Environment.</param>
        [Theory]
        [MemberData(nameof(OperationsData))]
        public async void MediaServicesV3ServiceCreateLocatorTest(RequestMediaServicesLocatorCreateDTO locatorCreateDTO, bool expectedValue, Type typeException, string environmentType)
        {
            // Arrange
            var storageService = Mock.Of<IStorageService>();
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var settingsProvider = Mock.Of<ISettingsProvider>();

            string assetName = "myassetname";
            var amsAccount = new MediaService(storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = MediaServicesV3PublicationTestData.DefaultStorageId } });

            // Arrange Mocks
            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.AssetFiltersCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), TimingIsWrong(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception());

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            Mock.Get(amsV3SdkWrapper)
                 .Setup(x => x.StreamingEndpointsListAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MockPageSe);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorListPathsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Paths);

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new StreamingLocator(assetName: "asset45", streamingPolicyName: locatorCreateDTO.StreamingPolicyName, defaultContentKeyPolicyName: locatorCreateDTO.ContentKeyPolicyName, contentKeys: new List<StreamingLocatorContentKey>()));

            if (locatorCreateDTO.ContentKeyPolicyName == MediaServicesV3PublicationTestData.GoodContentKeyPolicyDrm)
            {
                // let's add a CENC and CBCS keys
                Mock.Get(amsV3SdkWrapper)
                    .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new StreamingLocator(assetName: "asset45", streamingPolicyName: locatorCreateDTO.StreamingPolicyName, defaultContentKeyPolicyName: locatorCreateDTO.ContentKeyPolicyName, contentKeys: new List<StreamingLocatorContentKey>() { new StreamingLocatorContentKey(Guid.NewGuid(), StreamingLocatorContentKeyType.CommonEncryptionCenc), new StreamingLocatorContentKey(Guid.NewGuid(), StreamingLocatorContentKeyType.CommonEncryptionCbcs) }));
            }
            else
            {
                Mock.Get(amsV3SdkWrapper)
                     .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new StreamingLocator(assetName: "asset45", streamingPolicyName: locatorCreateDTO.StreamingPolicyName, defaultContentKeyPolicyName: locatorCreateDTO.ContentKeyPolicyName, contentKeys: new List<StreamingLocatorContentKey>()));
            }

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.AssetFiltersCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AssetFilter());

            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue(EnvironmentTypeConstants.EnvironmentTypeSettingName))
                .Returns(environmentType);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayPfxPassword"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayAskHex"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayCertificateB64"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic);

            Mock.Get(storageService)
                .Setup(x => x.ListBlobsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(MediaServicesV3PublicationTestData.GoodBlobItemListWithIsmFile);

            Mock.Get(storageService)
                .Setup(x => x.DownloadHttpRangeAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>(), default))
                .ReturnsAsync(MediaServicesV3PublicationTestData.GoodIsmFileBlobDownloadInfo);

            IMediaServicesV3ContentKeyPolicyService amsContentKeyPolService2 = new MediaServicesV3ContentKeyPolicyService(settingsProvider, LogKey);
            IMediaServicesV3CustomStreamingPolicyService amsStreamingPolService2 = new MediaServicesV3CustomStreamingPolicyService();

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService2, amsStreamingPolService2, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert
            if (expectedValue == false)
            {
                _ = await Xunit.Assert.ThrowsAsync(typeException, async () =>
                    await amsV3PubServices.LocatorCreateAsync(
                        locatorCreateDTO.ContainerUri,
                        locatorCreateDTO.StreamingPolicyName,
                        locatorCreateDTO.ContentKeyPolicyName,
                        locatorCreateDTO.TimeBasedFilter,
                        locatorCreateDTO.OperationContext,
                        locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                var result = await amsV3PubServices.LocatorCreateAsync(
                    locatorCreateDTO.ContainerUri,
                    locatorCreateDTO.StreamingPolicyName,
                    locatorCreateDTO.ContentKeyPolicyName,
                    locatorCreateDTO.TimeBasedFilter,
                    locatorCreateDTO.OperationContext,
                    locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false);
                result.ShouldBeOfType<ServiceOperationResultMediaServicesV3LocatorCreate>();
                result.LocatorName.ShouldBeOfType<string>();
            }
        }

        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsDataUpdatePolicy
        {
            get
            {
                return new[]
                {
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRM, "true", new ContentKeyPolicy(options: new List<ContentKeyPolicyOption>()) },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRM, "false", new ContentKeyPolicy(options: new List<ContentKeyPolicyOption>()) },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRM, "true", null },
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRM, "false", null },
                };
            }
        }


        /// <summary>
        /// Test the update of the content key policy.
        /// </summary>
        /// <param name="locatorCreateDTO">RequestorMediaServicesLocatorCreateDTO data.</param>
        /// <param name="updatePolicy">Set if behavior is to update the content key policy in AMS or not.</param>
        /// <param name="contentKeyPolicy">Content Key Policy existing or not in AMS.</param>
        [Theory]
        [MemberData(nameof(OperationsDataUpdatePolicy))]

        public async void MediaServicesV3ServiceCreateLocatorTestWithPolicyUpdate(RequestMediaServicesLocatorCreateDTO locatorCreateDTO, string updatePolicy, ContentKeyPolicy contentKeyPolicy)
        {
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var storageService = Mock.Of<IStorageService>();
            var settingsProvider = Mock.Of<ISettingsProvider>();

            // Arrange
            string assetName = "myassetname";
            var amsAccount = new MediaService(storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = MediaServicesV3PublicationTestData.DefaultStorageId } });

            // Arrange Mocks
            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingEndpointsListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockPageSe);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorListPathsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Paths);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StreamingLocator(assetName: "asset45", streamingPolicyName: locatorCreateDTO.StreamingPolicyName, defaultContentKeyPolicyName: locatorCreateDTO.ContentKeyPolicyName, contentKeys: new List<StreamingLocatorContentKey>() { new StreamingLocatorContentKey(Guid.NewGuid(), StreamingLocatorContentKeyType.CommonEncryptionCenc), new StreamingLocatorContentKey(Guid.NewGuid(), StreamingLocatorContentKeyType.CommonEncryptionCbcs) }));

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.ContentKeyPolicyGetAsync(locatorCreateDTO.ContentKeyPolicyName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contentKeyPolicy);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingPolicyGetAsync(MediaServicesV3PublicationTestData.GoodDRMStreamingPolicy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StreamingPolicy());

            Mock.Get(amsV3SdkWrapper)
               .Setup(x => x.ContentKeyPolicyCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ContentKeyPolicyOption>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument"))
                .Returns(MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument);

            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayPfxPassword"))
                .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword);

            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayAskHex"))
                .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex);

            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayCertificateB64"))
                .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmEnableContentKeyPolicyUpdate"))
             .Returns(updatePolicy);

            IMediaServicesV3ContentKeyPolicyService amsContentKeyPolService2 = new MediaServicesV3ContentKeyPolicyService(settingsProvider, LogKey);
            IMediaServicesV3CustomStreamingPolicyService amsStreamingPolService2 = new MediaServicesV3CustomStreamingPolicyService();

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService2, amsStreamingPolService2, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert
            var result = await amsV3PubServices.LocatorCreateAsync(
                locatorCreateDTO.ContainerUri,
                locatorCreateDTO.StreamingPolicyName,
                locatorCreateDTO.ContentKeyPolicyName,
                locatorCreateDTO.TimeBasedFilter,
                locatorCreateDTO.OperationContext,
                locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false);
            result.ShouldBeOfType<ServiceOperationResultMediaServicesV3LocatorCreate>();
            result.LocatorName.ShouldBeOfType<string>();
        }

        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsData2
        {
            get
            {
                return new[]
                {
                    new object[] { MediaServicesV3PublicationTestData.GoodDataDtoDRM, false, typeof(GridwichPublicationContentKeyPolicyException) },
                    new object[] { MediaServicesV3PublicationTestData.BadDataDtoFilterWhichThrowException, false, typeof(GridwichPublicationAssetFilterCreateException) },
                    new object[] { MediaServicesV3PublicationTestData.BadDataDtoAssetFilterWhichThrowException, false, typeof(GridwichPublicationMissingManifestFileException) }
                };
            }
        }


        /// <summary>
        /// Test the update of the content key policy with a failure.
        /// </summary>
        /// <param name="locatorCreateDTO">RequestorMediaServicesLocatorCreateDTO data.</param>
        /// <param name="expectedValue">Expected result.</param>
        /// <param name="typeException">Expected type of Exception.</param>
        [Theory]
        [MemberData(nameof(OperationsData2))]
        public async void MediaServicesV3ServiceCreateLocatorFailureTests(RequestMediaServicesLocatorCreateDTO locatorCreateDTO, bool expectedValue, Type typeException)
        {
            // Arrange
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var storageService = Mock.Of<IStorageService>();
            var amsContentKeyPolService = Mock.Of<IMediaServicesV3ContentKeyPolicyService>();
            var settingsProvider = Mock.Of<ISettingsProvider>();
            string assetName = "myassetname";
            var amsAccount = new MediaService(storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = MediaServicesV3PublicationTestData.DefaultStorageId } });

            // Arrange Mocks
            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue(EnvironmentTypeConstants.EnvironmentTypeSettingName))
                .Returns(EnvironmentTypeConstants.EnvironmentTypeDevelopment);

            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue("AmsDrmEnableContentKeyPolicyUpdate"))
                .Returns("true");

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.AssetFiltersCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), TimingIsWrong(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception());

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            Mock.Get(amsV3SdkWrapper)
                 .Setup(x => x.StreamingEndpointsListAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MockPageSe);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorListPathsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Paths);

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new StreamingLocator(assetName: "asset45", streamingPolicyName: locatorCreateDTO.StreamingPolicyName, defaultContentKeyPolicyName: locatorCreateDTO.ContentKeyPolicyName, contentKeys: new List<StreamingLocatorContentKey>()));

            if (locatorCreateDTO.ContentKeyPolicyName == MediaServicesV3PublicationTestData.GoodDataDtoDRM.ContentKeyPolicyName)
            {
                ContentKeyPolicy pol = null;

                Mock.Get(amsV3SdkWrapper)
                    .Setup(x => x.ContentKeyPolicyGetAsync(MediaServicesV3PublicationTestData.GoodDataDtoDRM.ContentKeyPolicyName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(pol);

                Mock.Get(amsV3SdkWrapper)
                    .Setup(x => x.ContentKeyPolicyCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<List<ContentKeyPolicyOption>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception());
            }

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetFiltersCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            Mock.Get(amsContentKeyPolService)
                .Setup(x => x.GetContentKeyPolicyFromMemory(It.IsAny<string>()))
                .Returns(new MediaServicesV3CustomContentKeyPolicyMultiDrmKey(
                    MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument,
                    MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword,
                    MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex,
                    MediaServicesV3PublicationTestData.AmsDrmFairPlayPfx));

            IMediaServicesV3CustomStreamingPolicyService amsStreamingPolService2 = new MediaServicesV3CustomStreamingPolicyService();

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService, amsStreamingPolService2, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert

            if (expectedValue)
            {
            }
            else
            {
                _ = await Xunit.Assert.ThrowsAsync(typeException, async () =>
                  await amsV3PubServices.LocatorCreateAsync(
                    locatorCreateDTO.ContainerUri,
                    locatorCreateDTO.StreamingPolicyName,
                    locatorCreateDTO.ContentKeyPolicyName,
                    locatorCreateDTO.TimeBasedFilter,
                    locatorCreateDTO.OperationContext,
                    locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test argument exception if openID settings is missing.
        /// </summary>
        [Fact]
        public void MediaServicesV3ServiceCreateLocatorTestWithOpenIdSettingsMissing()
        {
            var settingsProvider = Mock.Of<ISettingsProvider>();
            Mock.Get(settingsProvider)
           .Setup(x => x.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument"))
           .Returns((string)null);

            // Act
            var exception = Record.Exception(() => new MediaServicesV3ContentKeyPolicyService(settingsProvider, LogKey));

            // Assert
            Xunit.Assert.NotNull(exception);
            exception.ShouldBeOfType<ArgumentException>();
        }

        /// <summary>
        /// Test class is created if the openID setting is there.
        /// </summary>
        [Fact]
        public void MediaServicesV3ServiceCreateLocatorTestWithOpenIdSettings()
        {
            var settingsProvider = Mock.Of<ISettingsProvider>();

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmOpenIdConnectDiscoveryDocument"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmOpenIdConnectDiscoveryDocument);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayPfxPassword"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayPfxPassword);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayAskHex"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayAskHex);

            Mock.Get(settingsProvider)
             .Setup(x => x.GetAppSettingsValue("AmsDrmFairPlayCertificateB64"))
             .Returns(MediaServicesV3PublicationTestData.AmsDrmFairPlayCertAsSecretStringStatic);

            // Act
            var polService = new MediaServicesV3ContentKeyPolicyService(settingsProvider, LogKey);

            // Assert
            Xunit.Assert.NotNull(polService);
        }

        /// <summary>
        /// Test the failure of the creation of a locator.
        /// </summary>
        [Fact]
        public async void MediaServicesLocatorCreationFailureTest()
        {
            var locatorCreateDTO = MediaServicesV3PublicationTestData.RequestMediaServicesLocatorCreateDTO_Is_Expected;

            // Arrange
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var storageService = Mock.Of<IStorageService>();
            var amsContentKeyPolService = Mock.Of<IMediaServicesV3ContentKeyPolicyService>();
            var amsStreamingPolService = Mock.Of<IMediaServicesV3CustomStreamingPolicyService>();
            var settingsProvider = Mock.Of<ISettingsProvider>();
            string assetName = "myassetname";
            var amsAccount = new MediaService(storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = MediaServicesV3PublicationTestData.DefaultStorageId } });
            var streamingEndpoint = new StreamingEndpoint(name: "default", hostName: "test.azure.net", scaleUnits: 0);

            var mockPageSe = new MockPageCollection<StreamingEndpoint>(new List<StreamingEndpoint>() { streamingEndpoint }.ToList());

            // Arrange Mocks
            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue(EnvironmentTypeConstants.EnvironmentTypeSettingName))
                .Returns(EnvironmentTypeConstants.EnvironmentTypeDevelopment);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception());

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.AssetFiltersCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AssetFilter());

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService, amsStreamingPolService, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert
            _ = await Xunit.Assert.ThrowsAsync<GridwichPublicationLocatorCreationException>(async () =>
                await amsV3PubServices.LocatorCreateAsync(
                    locatorCreateDTO.ContainerUri,
                    locatorCreateDTO.StreamingPolicyName,
                    locatorCreateDTO.ContentKeyPolicyName,
                    locatorCreateDTO.TimeBasedFilter,
                    locatorCreateDTO.OperationContext,
                    locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false)).ConfigureAwait(false);
        }


        /// <summary>
        /// Test the failure of the list of streaming endpoints.
        /// </summary>
        [Fact]
        public async void MediaServicesLocatorStreamingEndpointListFailureTest()
        {
            var locatorCreateDTO = MediaServicesV3PublicationTestData.RequestMediaServicesLocatorCreateDTO_Is_Expected;

            // Arrange
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var storageService = Mock.Of<IStorageService>();
            var amsContentKeyPolService = Mock.Of<IMediaServicesV3ContentKeyPolicyService>();
            var amsStreamingPolService = Mock.Of<IMediaServicesV3CustomStreamingPolicyService>();
            var settingsProvider = Mock.Of<ISettingsProvider>();
            string assetName = "myassetname";
            var amsAccount = new MediaService(storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = MediaServicesV3PublicationTestData.DefaultStorageId } });
            var streamingEndpoint = new StreamingEndpoint(name: "default", hostName: "test.azure.net", scaleUnits: 0);

            var mockPageSe = new MockPageCollection<StreamingEndpoint>(new List<StreamingEndpoint>() { streamingEndpoint }.ToList());

            // Arrange Mocks
            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue(EnvironmentTypeConstants.EnvironmentTypeSettingName))
                .Returns(EnvironmentTypeConstants.EnvironmentTypeDevelopment);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingEndpointsListAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception());

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.AssetFiltersCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new AssetFilter());

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService, amsStreamingPolService, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert
            _ = await Xunit.Assert.ThrowsAsync<GridwichPublicationStreamingEndpointsListException>(async () =>
                await amsV3PubServices.LocatorCreateAsync(
                    locatorCreateDTO.ContainerUri,
                    locatorCreateDTO.StreamingPolicyName,
                    locatorCreateDTO.ContentKeyPolicyName,
                    locatorCreateDTO.TimeBasedFilter,
                    locatorCreateDTO.OperationContext,
                    locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Test the failure of the list paths.
        /// </summary>
        [Fact]
        public async void MediaServicesLocatorListPathsFailureTest()
        {
            var locatorCreateDTO = MediaServicesV3PublicationTestData.RequestMediaServicesLocatorCreateDTO_Is_Expected;

            // Arrange
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var storageService = Mock.Of<IStorageService>();
            var amsContentKeyPolService = Mock.Of<IMediaServicesV3ContentKeyPolicyService>();
            var amsStreamingPolService = Mock.Of<IMediaServicesV3CustomStreamingPolicyService>();
            var settingsProvider = Mock.Of<ISettingsProvider>();
            string assetName = "myassetname";
            var amsAccount = new MediaService(storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = MediaServicesV3PublicationTestData.DefaultStorageId } });
            var streamingEndpoint = new StreamingEndpoint(name: "default", hostName: "test.azure.net", scaleUnits: 0);

            var mockPageSe = new MockPageCollection<StreamingEndpoint>(new List<StreamingEndpoint>() { streamingEndpoint }.ToList());

            // Arrange Mocks
            Mock.Get(settingsProvider)
                .Setup(x => x.GetAppSettingsValue(EnvironmentTypeConstants.EnvironmentTypeSettingName))
                .Returns(EnvironmentTypeConstants.EnvironmentTypeDevelopment);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingEndpointsListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockPageSe2);

            Mock.Get(amsV3SdkWrapper)
              .Setup(x => x.StreamingLocatorCreateAsync(It.IsAny<string>(), It.IsAny<StreamingLocator>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new StreamingLocator());

            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorListPathsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService, amsStreamingPolService, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert
            _ = await Xunit.Assert.ThrowsAsync<GridwichPublicationListPathsException>(async () =>
                await amsV3PubServices.LocatorCreateAsync(
                    locatorCreateDTO.ContainerUri,
                    locatorCreateDTO.StreamingPolicyName,
                    locatorCreateDTO.ContentKeyPolicyName,
                    locatorCreateDTO.TimeBasedFilter,
                    locatorCreateDTO.OperationContext,
                    locatorCreateDTO.GenerateAudioFilters).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Testing the Media Services V3 Locator Delete feature.
        /// </summary>
        [Fact]
        public async void MediaServicesLocatorDeleteTest()
        {
            string locName = "locator-123";
            var amsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
            var storageService = Mock.Of<IStorageService>();
            var amsContentKeyPolService = Mock.Of<IMediaServicesV3ContentKeyPolicyService>();
            var amsStreamingPolService = Mock.Of<IMediaServicesV3CustomStreamingPolicyService>();
            var settingsProvider = Mock.Of<ISettingsProvider>();

            // Arrange Mocks
            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorDeleteAsync(locName, It.IsAny<CancellationToken>()));

            // Arrange Mocks
            Mock.Get(amsV3SdkWrapper)
                .Setup(x => x.StreamingLocatorDeleteAsync(null, It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new Exception());

            // Act
            var amsV3PubServices = new MediaServicesV3PublicationService(amsContentKeyPolService, amsStreamingPolService, amsV3SdkWrapper, storageService, settingsProvider, Log);

            // Assert
            var exception = await Record.ExceptionAsync(async () => await amsV3PubServices.LocatorDeleteAsync(locName, MediaServicesV3PublicationTestData.GoodOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
            Xunit.Assert.Null(exception); // no exception if string are not null

            // exception is locator is null
            await Xunit.Assert.ThrowsAsync<GridwichPublicationLocatorDeletionException>(async () => await amsV3PubServices.LocatorDeleteAsync(null, MediaServicesV3PublicationTestData.GoodOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}