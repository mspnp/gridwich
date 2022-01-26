using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.Exceptions;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Gridwich.SagaParticipants.Storage.AzureStorage.Services;
using Gridwich.SagaParticipants.Storage.AzureStorageTests.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;

using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.Services
{
    public class StorageServiceTests : IDisposable
    {
        private readonly IBlobBaseClientProvider _blobBaseClientProvider;
        private readonly IBlobContainerClientProvider _blobContainerClientProvider;
        private readonly IAzureStorageManagement _azureStorageManagement;
        private readonly IObjectLogger<StorageService> _logger;

        private readonly StorageService _storageService;

        // Note: Dispose is here solely to keep FxCop happy.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool b)
        {
            _storageService?.Dispose();
        }

        public StorageServiceTests()
        {
            _blobBaseClientProvider = Mock.Of<IBlobBaseClientProvider>();
            _blobContainerClientProvider = Mock.Of<IBlobContainerClientProvider>();
            _azureStorageManagement = Mock.Of<IAzureStorageManagement>();

            _logger = Mock.Of<IObjectLogger<StorageService>>();
            _storageService = new StorageService(_blobBaseClientProvider, _blobContainerClientProvider, _azureStorageManagement, _logger);
        }

        [Fact]
        public void GetSasUrlAsync_ShouldThrow_WhenNullUri()
        {
            // Arrange
            TimeSpan ttl = new TimeSpan(1, 0, 0);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _storageService.GetSasUrlForBlob(null, ttl, StorageClientProviderContext.None));
        }

        [Fact]
        public async void GetBlobPropertiesAsync_ShouldThrow_WhenNullUri()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.GetBlobPropertiesAsync(null, StorageClientProviderContext.None)).ConfigureAwait(false);
        }

        [Fact]
        public async void GetBlobPropertiesAsync_ShouldThrow_WhenClientOperationFails()
        {
            // Arrange
            var blobUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/test-earth-video-26.mp4");
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var context = StorageClientProviderContext.None;
            var sleeveMock = MockSleeve(blobClientMock, context);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(blobUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.GetBlobPropertiesAsync(blobUri, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToGetBlobPropertiesInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async void GetBlobPropertiesAsync_ShouldReturnProperties_WhenOperationSucceeds()
        {
            // Arrange
            var blobUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/test-earth-video-26.mp4");
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var context = StorageClientProviderContext.None;
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<BlobProperties>>();
            var blobPropertiesMock = Mock.Of<BlobProperties>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(blobUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(blobPropertiesMock);

            // Act
            var result = await _storageService.GetBlobPropertiesAsync(blobUri, context).ConfigureAwait(false);

            // Assert
            result.ShouldBe(blobPropertiesMock);
        }

        [Fact]
        public async void GetBlobMetadataAsync_ShouldThrow_WhenNullUri()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.GetBlobMetadataAsync(null, StorageClientProviderContext.None)).ConfigureAwait(false);
        }

        [Fact]
        public async void GetBlobMetadataAsync_ShouldReturnProperties_WhenOperationSucceeds()
        {
            // Arrange
            var blobUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/test-earth-video-26.mp4");
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var context = StorageClientProviderContext.None;
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<BlobProperties>>();
            var blobProperties = new BlobProperties() { Metadata = { { "key", "value" } } };

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(blobUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(blobProperties);

            // Act
            var result = await _storageService.GetBlobMetadataAsync(blobUri, context).ConfigureAwait(false);

            // Assert
            result.ShouldBeEquivalentTo(JObject.FromObject(new Dictionary<string, string>() { { "key", "value" } }));
        }

        [Fact]
        public async void DownloadHttpRangeAsync_ShouldThrow_WhenNullUri()
        {
            // Arrange
            var context = TestHelpers.CreateGUIDContext();
            var httpRange = new HttpRange(0);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.DownloadHttpRangeAsync(null, context, httpRange)).ConfigureAwait(false);
        }

        [Fact]
        public async void DownloadHttpRangeAsync_ShouldThrow_WhenClientOperationFails()
        {
            // Arrange
            var blobUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/test-earth-video-26.mp4");
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var context = StorageClientProviderContext.None;
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<BlobDownloadInfo>>();
            var httpRange = default(HttpRange);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(blobUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.DownloadAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() =>
                _storageService.DownloadHttpRangeAsync(blobUri, context, httpRange)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToDownloadHttpRangeInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async void DownloadHttpRangeAsync_ShouldReturnInfo_WhenOperationSucceeds()
        {
            // Arrange
            var blobUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/test-earth-video-26.mp4");
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var context = StorageClientProviderContext.None;
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<BlobDownloadInfo>>();
            var httpRange = default(HttpRange);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(blobUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.DownloadAsync(It.IsAny<HttpRange>(), It.IsAny<BlobRequestConditions>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(default(BlobDownloadInfo));

            // Act
            var result = await _storageService.DownloadHttpRangeAsync(blobUri, context, httpRange).ConfigureAwait(false);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async void SetBlobMetadataAsyncAsync_ShouldThrow_WhenNullUri()
        {
            // Arrange
            var context = TestHelpers.CreateGUIDContext();
            var metadataData = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.SetBlobMetadataAsync(null, metadataData, context)).ConfigureAwait(false);
        }

        [Fact]
        public async void SetBlobMetadataAsyncAsync_ShouldThrow_WhenNullMetadata()
        {
            // Arrange
            var testUri = new Uri("https://www.topichost.com");
            var context = TestHelpers.CreateGUIDContext();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.SetBlobMetadataAsync(testUri, null, context)).ConfigureAwait(false);
        }

        [Fact]
        public async void SetBlobMetadataAsyncAsync_ShouldThrow_WhenEmptyMetadata()
        {
            // Arrange
            var testUri = new Uri("https://www.topichost.com");
            var metadataData = new Dictionary<string, string>();
            var context = TestHelpers.CreateGUIDContext();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.SetBlobMetadataAsync(testUri, metadataData, context)).ConfigureAwait(false);
        }

        [Fact]
        public async void SetBlobMetadataAsyncAsync_ShouldThrow_WhenBlobDoesNotExist()
        {
            // Arrange
            var testUri = new Uri("https://www.topichost.com");
            var metadataData = new Dictionary<string, string>() { { "key", "value" } };
            var context = TestHelpers.CreateGUIDContext();

            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<bool>>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(testUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.SetBlobMetadataAsync(testUri, metadataData, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogEventObject(LogEventIds.StorageServiceAttemptToAddMetadataToBlobThatDoesNotExist, It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async void SetBlobMetadataAsyncAsync_ShouldThrow_WhenClientOperationThrows()
        {
            // Arrange
            var testUri = new Uri("https://www.topichost.com");
            var metadataData = new Dictionary<string, string>() { { "key", "value" } };
            var context = TestHelpers.CreateGUIDContext();

            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<bool>>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(testUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(true);
            Mock.Get(blobClientMock)
                .Setup(x => x.SetMetadataAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.SetBlobMetadataAsync(testUri, metadataData, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToSetMetadataInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async void SetBlobMetadataAsyncAsync_ShouldReturnTrue_WhenOperationSucceeds()
        {
            // Arrange
            var testUri = new Uri("https://www.topichost.com");
            var metadataData = new Dictionary<string, string>() { { "key", "value" } };
            var context = TestHelpers.CreateGUIDContext();

            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<bool>>();
            var blobInfoResponse = Mock.Of<Response<BlobInfo>>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(testUri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(true);
            Mock.Get(blobClientMock)
                .Setup(x => x.SetMetadataAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(blobInfoResponse);

            // Act
            var result = await _storageService.SetBlobMetadataAsync(testUri, metadataData, context).ConfigureAwait(false);

            // Assert
            result.ShouldBe(true);
        }

        [Fact]
        public void GetSasUrlAsync_ShouldThrow_WhenConnectionStringInvalid()
        {
            // Arrange
            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "core.windows.net";
            var expectedContainer = "container1";
            var expectedBlob = "path1/blob1.mp4";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.{expectedEndpointSuffix}/{expectedContainer}/{expectedBlob}");
            TimeSpan ttl = new TimeSpan(1, 0, 0);

            // Act & Assert
            Assert.Throws<GridwichStorageServiceException>(() =>
                _storageService.GetSasUrlForBlob(uri1, ttl, StorageClientProviderContext.None));
        }

        [Fact]
        public void GetSasUrlAsync_ShouldThrow_WhenSasCreationFails()
        {
            // Arrange
            Uri blobUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/test-earth-video-26.mp4");
            TimeSpan ttl = new TimeSpan(1, 0, 0);

            // Act & Assert
            Assert.Throws<GridwichStorageServiceException>(() =>
                _storageService.GetSasUrlForBlob(blobUri, ttl, StorageClientProviderContext.None));
        }

        [Fact]
        public async Task ChangeBlobTier_ShouldThrow_WhenBlobUriIsNull()
        {
            var context = TestHelpers.CreateGUIDContext();
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                        await _storageService.ChangeBlobTierAsync(null, BlobAccessTier.Archive,
                                BlobRehydratePriority.Normal, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ChangeBlobTier_ShouldThrow_When_SetAccessTierThrows()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = TestHelpers.CreateGUIDContext();
            var tier = BlobAccessTier.Hot;
            var hydration = BlobRehydratePriority.Normal;

            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(sleeveMock)
                .Setup(x => x.Client.SetAccessTierAsync(tier.ToAzureEnum(), null, hydration.ToAzureEnum(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("some exception"));

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.ChangeBlobTierAsync(uri, tier, hydration, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToChangeTierInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangeBlobTier_ShouldReturnFalse_WhenTierChangeFailed()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = TestHelpers.CreateGUIDContext();
            var tier = BlobAccessTier.Archive;
            var hydration = BlobRehydratePriority.Normal;

            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.SetAccessTierAsync(tier.ToAzureEnum(), It.IsAny<BlobRequestConditions>(), hydration.ToAzureEnum(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Status)
                .Returns(500);

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() =>
                _storageService.ChangeBlobTierAsync(uri, tier, hydration, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogEventObject(LogEventIds.ChangeBlobTierUnexpectedResponse, It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangeBlobTier_ShouldReturnTrue_WhenTierChangeSucceeds()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = TestHelpers.CreateGUIDContext();
            var tier = BlobAccessTier.Cool;
            var hydration = BlobRehydratePriority.Normal;

            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.SetAccessTierAsync(tier.ToAzureEnum(), null, hydration.ToAzureEnum(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Status)
                .Returns(200);

            // Act
            var result = await _storageService.ChangeBlobTierAsync(uri, tier, hydration, context).ConfigureAwait(false);

            // Assert:

            Mock.Get(_logger)
                .Verify(x => x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<Uri>()), Times.Never);

            Mock.Get(_logger)
                .Verify(x => x.LogEventObject(It.IsAny<EventId>(), It.IsAny<object>()), Times.Never);

            result.ShouldBe(true);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4", null)]
        [InlineData(null, "https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4")]
        public async Task BlobCopy_ShouldThrowException_WhenSourceAndOrDestUriIsNull(string source, string dest)
        {
            // Arrange
            Uri sourceUri = null;
            Uri destUri = null;
            if (source != null)
            {
                sourceUri = new Uri(source);
            }

            if (dest != null)
            {
                destUri = new Uri(dest);
            }

            var context = new StorageClientProviderContext("{ \"a\" : 1 }", false, string.Empty);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storageService.BlobCopy(sourceUri, destUri, context)).ConfigureAwait(false);
        }

        [Fact]
        public async Task BlobCopy_ShouldThrow_When_CreateContainerThrows()
        {
            // Arrange
            var sourceUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/alexa_play_despacito.mp4");
            var destUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/alexa_play_despacito_again.mp4");

            var context = new StorageClientProviderContext("{ \"xxx\" : 1232 }", false, string.Empty);
            var destinationClient = Mock.Of<BlobBaseClient>();
            var destinationSleeve = MockSleeve(destinationClient, context);
            var containerClient = Mock.Of<BlobContainerClient>();
            var containerSleeve = MockContainerSleeve(containerClient, context);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(destinationSleeve);
            Mock.Get(_blobContainerClientProvider)
                .Setup(x => x.GetBlobContainerSleeveForUri(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(containerSleeve);
            Mock.Get(containerClient)
                .Setup(x => x.CreateIfNotExistsAsync(
                        It.IsAny<PublicAccessType>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<BlobContainerEncryptionScopeOptions>(),
                        It.IsAny<CancellationToken>()))
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.BlobCopy(sourceUri, destUri, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.StorageServiceFailedCreatingTargetContainerForBlobCopy, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task BlobCopy_ShouldThrow_When_StartingCopyThrows()
        {
            // Arrange
            var sourceUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/alexa_play_despacito.mp4");
            var destUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/alexa_play_despacito_again.mp4");

            var context = new StorageClientProviderContext("{ \"xxx\" : 1232 }", false, string.Empty);
            var destinationClient = Mock.Of<BlobBaseClient>();
            var destinationSleeve = MockSleeve(destinationClient, context);
            var containerClient = Mock.Of<BlobContainerClient>();
            var containerSleeve = MockContainerSleeve(containerClient, context);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(destinationSleeve);
            Mock.Get(_blobContainerClientProvider)
                .Setup(x => x.GetBlobContainerSleeveForUri(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(containerSleeve);
            Mock.Get(containerClient)
                .Setup(x => x.CreateIfNotExistsAsync(
                        It.IsAny<PublicAccessType>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<CancellationToken>()))
               .ReturnsAsync(default(Response<BlobContainerInfo>));
            Mock.Get(destinationClient)
                .Setup(x => x.StartCopyFromUriAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<AccessTier?>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<RehydratePriority?>(),
                    It.IsAny<CancellationToken>()))
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.BlobCopy(sourceUri, destUri, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToCopyBlobInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ContainerCreate_ShouldThrow_WhenStorageAccountNameIsNull()
        {
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.ContainerCreateAsync(null, "Container-Name", StorageClientProviderContext.None).ConfigureAwait(false)).ConfigureAwait(false);
        }
        [Fact]
        public async Task ContainerCreate_ShouldThrow_WhenContainerNameIsNull()
        {
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.ContainerCreateAsync("StorageAccount", null, StorageClientProviderContext.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ContainerDelete_ShouldThrow_WhenStorageAccountNameIsNull()
        {
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.ContainerDeleteAsync(null, "Container-Name", StorageClientProviderContext.None).ConfigureAwait(false)).ConfigureAwait(false);
        }
        [Fact]
        public async Task ContainerDelete_ShouldThrow_WhenContainerNameIsNull()
        {
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.ContainerDeleteAsync("StorageAccount", null, StorageClientProviderContext.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("gridwichinbox00sb", null)]
        [InlineData("gridwichinbox00sb", "onpremauto")]
        public async Task ContainerSetPublicAccessAsync_ShouldThrow_WhenArgumentNull(string accountName, string containerName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _storageService.ContainerSetPublicAccessAsync(accountName, containerName, ContainerAccessType.Blob, null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ContainerSetPublicAccessAsync_ShouldRunWithoutExceptions()
        {
            var accessType = ContainerAccessType.Blob;
            var context = StorageClientProviderContext.None;
            var containerClient = Mock.Of<BlobContainerClient>();
            var containerSleeve = MockContainerSleeve(containerClient, context);

            Mock.Get(_azureStorageManagement)
                .Setup(x => x.GetAccountKey("gridwichinbox00sb"))
                .Returns("ACCOUNT_KEY");
            Mock.Get(_blobContainerClientProvider)
                .Setup(x => x.GetBlobContainerSleeveForConnectionString(It.IsAny<string>(), "onpremauto", context))
                .Returns(containerSleeve);
            Mock.Get(containerClient)
                .Setup(x => x.SetAccessPolicyAsync(accessType.ToAzureEnum(), null, null, CancellationToken.None))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            await _storageService.ContainerSetPublicAccessAsync("gridwichinbox00sb", "onpremauto", ContainerAccessType.Blob, context).ConfigureAwait(false);
        }

        [Fact]
        public async Task ContainerSetPublicAccessAsync_ShouldThrow_StorageException()
        {
            var accessType = ContainerAccessType.Blob;
            var context = StorageClientProviderContext.None;
            var containerClient = Mock.Of<BlobContainerClient>();
            var containerSleeve = MockContainerSleeve(containerClient, context);

            Mock.Get(_azureStorageManagement)
                .Setup(x => x.GetAccountKey("gridwichinbox00sb"))
                .Returns("ACCOUNT_KEY");
            Mock.Get(_blobContainerClientProvider)
                .Setup(x => x.GetBlobContainerSleeveForConnectionString(It.IsAny<string>(), "onpremauto", context))
                .Returns(containerSleeve);
            Mock.Get(containerClient)
                .Setup(x => x.SetAccessPolicyAsync(accessType.ToAzureEnum(), null, null, CancellationToken.None))
                .ThrowsAsync(new ArgumentException("mock"));

            await Assert.ThrowsAsync<GridwichStorageServiceException>(
                async () => await _storageService.ContainerSetPublicAccessAsync("gridwichinbox00sb", "onpremauto", ContainerAccessType.Blob, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("gridwichinbox00sb")]
        public void GetConnStringForAccount_ShouldThrow_WhenArgumentsNull(string accountName)
        {
            Assert.Throws<ArgumentNullException>(() => _storageService.GetConnectionStringForAccount(accountName, null));
        }

        [Fact]
        public void GetConnStringForAccount_ShouldReturn_ConnectionString()
        {
            var context = StorageClientProviderContext.None;

            Mock.Get(_azureStorageManagement)
                .Setup(x => x.GetAccountKey("gridwichinbox00sb"))
                .Returns("ACCOUNT_KEY");

            var result = _storageService.GetConnectionStringForAccount("gridwichinbox00sb", context);

            result.ShouldNotBeNull();
        }

        [Fact]
        public void GetConnStringForAccount_ShouldThrow_IfNoAccountKeyAvailable()
        {
            var context = StorageClientProviderContext.None;

            Mock.Get(_azureStorageManagement)
                .Setup(x => x.GetAccountKey("gridwichinbox00sb"))
                .Returns((string)null);

            Assert.Throws<GridwichStorageServiceException>(() => _storageService.GetConnectionStringForAccount("gridwichinbox00sb", context));
        }


        // Arrange
        [Theory]
        [InlineData(true, "https://storeageaccountname.blob.core.windows.net/containername", "/filepath/filename.mp4", "https://storeageaccountname.blob.core.windows.net/containername/filepath/filename.mp4")]
        [InlineData(true, "https://storeageaccountname.blob.core.windows.net/containername", "filepath/filename.mp4", "https://storeageaccountname.blob.core.windows.net/containername/filepath/filename.mp4")]
        [InlineData(true, "https://storeageaccountname.blob.core.windows.net/containername/", "/filepath/filename.mp4", "https://storeageaccountname.blob.core.windows.net/containername/filepath/filename.mp4")]
        [InlineData(false, "https://storeageaccountname.blob.core.windows.net", "/filepath/filename.mp4", null)]
        public void CreateBlobUrl_ShouldAlwaysProduceExpectedValue(bool shouldBeValid, string containerName, string blobName, string expectedValue)
        {
            Uri result = null;
            try
            {
                // Act
                result = _storageService.CreateBlobUrl(containerName, blobName);
            }
            catch (ArgumentException)
            {
                shouldBeValid.ShouldBeFalse();

                Mock.Get(_logger).Verify(x =>
                    x.LogEvent(LogEventIds.StorageServiceFailingUriMissingContainer, It.IsAny<string>()),
                    Times.Once);
            }
            catch (Exception)
            {
                false.ShouldBeTrue(); // Should not get any other exception
            }

            // Assert
            if (shouldBeValid)
            {
                result.ShouldNotBeNull();
                result.ToString().ShouldBe(expectedValue);

                // and no logging
                Mock.Get(_logger).Verify(x =>
                    x.LogEvent(It.IsAny<EventId>(), It.IsAny<string>()),
                    Times.Never);
            }
            else
            {
                result.ShouldBeNull();
            }
        }

        [Fact]
        public async Task GetBlobExists_ShouldThrow_WhenBlobUriIsNull()
        {
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.GetBlobExistsAsync(null, null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task GetBlobExists_ShouldThrow_WhenExistsAsyncThrows()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("some exception"));

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(() => _storageService.GetBlobExistsAsync(uri, context)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToCheckBlobExistenceDueToStorageExceptionInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetBlobExists_ShouldReturnTrue_WhenBlobExists()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            var blobClientMock = Mock.Of<BlobBaseClient>();
            var sleeveMock = MockSleeve(blobClientMock, context);
            var responseMock = Mock.Of<Response<bool>>();

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(blobClientMock)
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock);
            Mock.Get(responseMock)
                .Setup(x => x.Value)
                .Returns(true);

            // Act
            var result = await _storageService.GetBlobExistsAsync(uri, context).ConfigureAwait(false);

            // Assert:
            result.ShouldBe(true);
        }

        [Fact]
        public async Task ListBlobsAsync_ShouldThrow_WhenBlobUriIsNull()
        {
            // Act & Assert:
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.ListBlobsAsync(null, null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ListBlobsAsync_ShouldThrow_WhenEnumerationFails()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            var containerClientMock = Mock.Of<BlobContainerClient>();
            var sleeveMock = MockContainerSleeve(containerClientMock, context);

            Mock.Get(_blobContainerClientProvider)
                .Setup(x => x.GetBlobContainerSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(containerClientMock)
                .Setup(x => x.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(async () => await _storageService.ListBlobsAsync(uri, context).ConfigureAwait(false)).ConfigureAwait(false);

            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToListBlobsDueToStorageExceptionInStorageService,
                        It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "A critical error should be logged when the storage account fails to enumerate blobs.");
        }

        [Fact]
        public async Task ListBlobsAsync_ShouldReturnBlobs_WhenEnumerationSucceeds()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            var containerClientMock = Mock.Of<BlobContainerClient>();
            var sleeveMock = MockContainerSleeve(containerClientMock, context);
            var blobItem = Mock.Of<BlobItem>();
            var testAsyncPageable = new TestAsyncPageable<BlobItem>(new List<BlobItem>() { blobItem });

            Mock.Get(_blobContainerClientProvider)
                .Setup(x => x.GetBlobContainerSleeveForUri(uri, context))
                .Returns(() => sleeveMock);
            Mock.Get(containerClientMock)
                .Setup(x => x.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(testAsyncPageable);

            // Act
            var result = await _storageService.ListBlobsAsync(uri, context).ConfigureAwait(false);
            var blobList = new List<BlobItem>(result);

            // Assert
            blobList.Count.ShouldBe(1);
            result.ShouldContain(blobItem);
        }

        [Fact]
        public async Task GetOrDownloadContentAsync_ShouldThrow_WhenUriIsNull()
        {
            // Arrange
            var context = StorageClientProviderContext.None;
            var desiredOffset = 0;
            var desiredSize = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.GetOrDownloadContentAsync(null, desiredOffset, desiredSize, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task GetOrDownloadContentAsync_ShouldThrow_WhenOffsetIsInvalid()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            long desiredOffset = -5;
            long desiredSize = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _storageService.GetOrDownloadContentAsync(uri, desiredOffset, desiredSize, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task GetOrDownloadContentAsync_ShouldThrow_WhenSizeIsInvalid()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            long desiredOffset = 5;
            long desiredSize = -5;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _storageService.GetOrDownloadContentAsync(uri, desiredOffset, desiredSize, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task GetOrDownloadContentAsync_ShouldThrow_WhenSizeIsMoreThanMax()
        {
            // Arrange
            var uri = new Uri("https://gridwichasset00sasb.com/container1/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            long desiredOffset = 5;
            long desiredSize = long.MaxValue;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _storageService.GetOrDownloadContentAsync(uri, desiredOffset, desiredSize, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task BlobDelete_ShouldThrow_WhenUriIsNull()
        {
            // Arrange
            var context = StorageClientProviderContext.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storageService.BlobDelete(null, context).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task BlobDelete_ShouldThrow_WhenClientOperationFails()
        {
            // Arrange
            var uri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/alexa_play_despacito.mp4");
            var context = StorageClientProviderContext.None;
            var blobBaseClient = Mock.Of<BlobBaseClient>();
            var mockSleeve = MockSleeve(blobBaseClient, context);

            Mock.Get(_blobBaseClientProvider)
                .Setup(x => x.GetBlobClientSleeveForUri(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(mockSleeve);
            Mock.Get(blobBaseClient)
                .Setup(x => x.DeleteIfExistsAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .Throws<ArgumentException>();

            // Act & Assert
            await Assert.ThrowsAsync<GridwichStorageServiceException>(async () => await _storageService.BlobDelete(uri, context).ConfigureAwait(false)).ConfigureAwait(false);
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(LogEventIds.FailedToDeleteDueToStorageExceptionInStorageService, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }

        /// <summary>
        /// Create a mock sleeve, properly set up so that the two main getter's work.
        /// </summary>
        private static IStorageBlobClientSleeve MockSleeve(BlobBaseClient client, StorageClientProviderContext context)
        {
            Mock.Get(client)
                .SetupGet(x => x.AccountName)
                .Returns(@"ut");

            var sleeve = Mock.Of<IStorageBlobClientSleeve>();
            Mock.Get(sleeve)
                .SetupGet(x => x.Client)
                .Returns(client);
            Mock.Get(sleeve)
                .SetupGet(x => x.Context)
                .Returns(context);

            return sleeve;
        }

        /// <summary>
        /// Create a mock container sleeve, properly set up so that the two main getter's work.
        /// </summary>
        private static IStorageContainerClientSleeve MockContainerSleeve(BlobContainerClient client, StorageClientProviderContext context)
        {
            Mock.Get(client)
                .SetupGet(x => x.AccountName)
                .Returns(@"ut");

            var sleeve = Mock.Of<IStorageContainerClientSleeve>();
            Mock.Get(sleeve)
                .SetupGet(x => x.Client)
                .Returns(client);
            Mock.Get(sleeve)
                .SetupGet(x => x.Context)
                .Returns(context);

            return sleeve;
        }
    }
}
