using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions;
using Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Services;
using Gridwich.SagaParticipants.Analysis.MediaInfoTests.Utils;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Analysis.MediaInfoTests.MediaInfoProviders
{
    public class MediaInfoReportServiceTests
    {
        private readonly IMediaInfoProvider _mediaInfoProvider;
        private readonly IStorageService _storageService;
        private readonly IObjectLogger<MediaInfoReportService> _logger;
        private readonly IMediaInfoReportService _service;

        public MediaInfoReportServiceTests()
        {
            _mediaInfoProvider = Mock.Of<IMediaInfoProvider>();
            _storageService = Mock.Of<IStorageService>();
            _logger = Mock.Of<IObjectLogger<MediaInfoReportService>>();
            _service = new MediaInfoReportService(
                _mediaInfoProvider,
                _storageService,
                _logger);
        }

        private static void SetPropertyValue(object obj, string propertyName, object value)
        {
            PropertyInfo propertyInfo = obj.GetType().GetTypeInfo().GetProperty(propertyName);
            propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType, CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_Throw_When_BlobUriIsNull()
        {
            // Arrange
            StorageClientProviderContext context = StorageClientProviderContext.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.GetMediaInfoCompleteInformForUriAsync(null, context).ConfigureAwait(true))
                .ConfigureAwait(true);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_Throw_When_ContextIsNull()
        {
            // Arrange
            Uri uri = new Uri("http://free.corona.com");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await _service.GetMediaInfoCompleteInformForUriAsync(uri, null).ConfigureAwait(true))
                .ConfigureAwait(true);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Return_Null_If_ContentLength_Zero()
        {
            // Arrange
            var uri = new Uri("https://test.com/uri");
            var mediaInfo = Mock.Of<IMediaInfoService>();
            var blobProperties = Mock.Of<BlobProperties>();
            SetPropertyValue(blobProperties, "ContentLength", 0);
            var context = TestHelpers.CreateGUIDContext();
            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(() => mediaInfo);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, context))
                .ReturnsAsync(() => blobProperties);

            // Act & Assert
            await Assert.ThrowsAsync<GridwichMediaInfoLibException>(async () => await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.InvalidBlobContentLength, uri), Times.Once);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Throws_InvalidContentException()
        {
            // Arrange
            var uri = new Uri("https://test.com/uri");
            var mediaInfo = Mock.Of<IMediaInfoService>();
            var blobProperties = Mock.Of<BlobProperties>();
            var context = TestHelpers.CreateGUIDContext();
            SetPropertyValue(blobProperties, "ContentLength", 10);
            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(() => mediaInfo);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);

            // Act & Assert
            await Assert.ThrowsAsync<GridwichMediaInfoInvalidContentException>(async () => await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoInvalidContent, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_Throw_When_OpenBufferInitThrows()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferInit(contentLength, contentOffset))
                .Throws(new ArgumentException("Error"));

            // Act & Assert
            await Assert.ThrowsAsync<GridwichMediaInfoLibUnexpectedException>(async () => await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x => x.LogExceptionObject(LogEventIds.MediaInfoLibOpenBufferInitFailed, It.IsAny<Exception>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_Throw_When_OpenBufferContinue()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Throws(new ArgumentException("Error"));

            // Act & Assert
            await Assert.ThrowsAsync<GridwichMediaInfoLibUnexpectedException>(async () => await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x => x.LogExceptionObject(LogEventIds.MediaInfoLibOpenBufferContinueFailed, It.IsAny<Exception>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_ReturnInform_When_Finalized()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);
            IntPtr status = (IntPtr)MediaInfoReportService.MediaInfoStatus.Finalized;
            JObject informJObject = JObject.Parse("{\"test\":1}");

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns(status);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.GetInform())
                .Returns(informJObject.ToString);

            // Act
            var result = await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true);

            // Assert
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoFileReadFinalized, It.IsAny<object>()), Times.Once);
            result.ShouldBeEquivalentTo(informJObject);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_Throw_When_InformIsNull()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);
            IntPtr status = (IntPtr)MediaInfoReportService.MediaInfoStatus.Finalized;

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns(status);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.GetInform())
                .Returns((string)null);

            // Act & Assert
            await Assert.ThrowsAsync<GridwichMediaInfoLibException>(async () => await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.InvalidMediaInfoLibReport, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_Throw_When_OpenBufferContinueGoToGetThrows()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);
            IntPtr status = (IntPtr)MediaInfoReportService.MediaInfoStatus.Accepted;

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns(status);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinueGoToGet())
                .Throws(new ArgumentException("Error"));

            // Act & Assert
            await Assert.ThrowsAsync<GridwichMediaInfoLibUnexpectedException>(async () => await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x => x.LogExceptionObject(LogEventIds.MediaInfoLibOpenBufferContinueGoToGetFailed, It.IsAny<Exception>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_ReturnInform_When_FinalizedWithDesiredOffset()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);
            IntPtr status = (IntPtr)MediaInfoReportService.MediaInfoStatus.Accepted;
            JObject informJObject = JObject.Parse("{\"test\":1}");

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns(status);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.GetInform())
                .Returns(informJObject.ToString);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinueGoToGet())
                .Returns(contentLength);

            // Act
            var result = await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true);

            // Assert
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoRequestedEndOfFile, It.IsAny<object>()), Times.Once);
            result.ShouldBeEquivalentTo(informJObject);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_ReturnInform_When_FinalizedMismatchOffset()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);
            JObject informJObject = JObject.Parse("{\"test\":1}");

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, 0,
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .SetupSequence(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns((IntPtr)MediaInfoReportService.MediaInfoStatus.Accepted)
                .Returns((IntPtr)MediaInfoReportService.MediaInfoStatus.Finalized);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.GetInform())
                .Returns(informJObject.ToString);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinueGoToGet())
                .Returns(MediaInfoReportService.MediaInfoReadForward);

            // Act
            var result = await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true);

            // Assert
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoReadNewRangeRequested, It.IsAny<object>()), Times.Once);
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoMismatchInDesiredOffset, It.IsAny<object>()), Times.Once);
            result.ShouldBeEquivalentTo(informJObject);
        }

        [Fact]
        public async Task GetMediaInfoCompleteInformForUriAsync_Should_ReturnInform_When_FinalizedWithReadForward()
        {
            // Arrange
            var mediaInfoMock = Mock.Of<IMediaInfoService>();
            var memoryStreamMock = Mock.Of<MemoryStream>();
            var contentOffset = 10;
            var contentLength = 100;
            var uri = new Uri("https://test.com/uri");
            var context = TestHelpers.CreateGUIDContext();
            var blobProperties = MediaInfoTestUtils.CreateBlobProperties(contentLength);
            var content = new CachedHttpRangeContent(new HttpRange(contentOffset, contentLength), memoryStreamMock);
            JObject informJObject = JObject.Parse("{\"test\":1}");

            Mock.Get(_mediaInfoProvider)
                .Setup(x => x.GetMediaInfoLib(context))
                .Returns(mediaInfoMock);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobPropertiesAsync(uri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(() => blobProperties);
            Mock.Get(_storageService)
                .Setup(x => x.GetOrDownloadContentAsync(uri, It.IsAny<long>(),
                    IStorageService.UseDefaultLength, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(content);
            Mock.Get(mediaInfoMock)
                .SetupSequence(x => x.OpenBufferContinue(It.IsAny<IntPtr>(), It.IsAny<IntPtr>()))
                .Returns((IntPtr)MediaInfoReportService.MediaInfoStatus.Accepted)
                .Returns((IntPtr)MediaInfoReportService.MediaInfoStatus.Finalized);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.GetInform())
                .Returns(informJObject.ToString);
            Mock.Get(mediaInfoMock)
                .Setup(x => x.OpenBufferContinueGoToGet())
                .Returns(10);

            // Act
            var result = await _service.GetMediaInfoCompleteInformForUriAsync(uri, context).ConfigureAwait(true);

            // Assert
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoSeekRequested, It.IsAny<object>()), Times.Once);
            Mock.Get(_logger).Verify(x => x.LogEventObject(LogEventIds.MediaInfoFileReadFinalized, It.IsAny<object>()), Times.Once);
            result.ShouldBeEquivalentTo(informJObject);
        }
    }
}
