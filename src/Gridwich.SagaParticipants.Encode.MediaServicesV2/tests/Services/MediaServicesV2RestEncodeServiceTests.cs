using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV2.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2Tests.Services
{
    public class MediaServicesV2RestEncodeServiceTests
    {
        private readonly IObjectLogger<MediaServicesV2RestEncodeService> _log;
        private readonly IStorageService _storageService;
        private readonly IMediaServicesV2RestWrapper _mediaServicesV2RestWrapper;
        private readonly MediaServicesV2RestEncodeService _service;

        public MediaServicesV2RestEncodeServiceTests()
        {
            _log = Mock.Of<IObjectLogger<MediaServicesV2RestEncodeService>>();
            _storageService = Mock.Of<IStorageService>();
            _mediaServicesV2RestWrapper = Mock.Of<IMediaServicesV2RestWrapper>();
            _service = new MediaServicesV2RestEncodeService(
                _log,
                _storageService,
                _mediaServicesV2RestWrapper);
        }

        private readonly string emptyAssetId = "nb:cid:UUID:7e712f7e-d35f-4629-a9b1-47b35b2b576b";
        private readonly Uri emptyAssetUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/asset-7e712f7e-d35f-4629-a9b1-47b35b2b576b");
        private (string AssetId, Uri AssetUri) GetTupleFor_CreateEmptyAssetAsync()
        {
            return (emptyAssetId, emptyAssetUri);
        }

        private readonly string firstTaskId = "nb:tid:UUID:7e712f7e-d35f-4629-a9b1-47b35b2b576b";
        private (string FirstTaskId, string FirstTaskName) GetTupleFor_GetFirstTaskAsync(string firstTaskNameExpected)
        {
            return (firstTaskId, firstTaskNameExpected);
        }

        private readonly string firstOutputAssetId = "nb:cid:UUID:7e712f7e-d35f-1111-a9b1-47b35b2b576b";
        private readonly string firstOutputAssetName = "someAssetName";
        private (string FirstOutputAssetId, string FirstOutputAssetName) GetTupleFor_GetFirstOutputAssetAsync()
        {
            return (firstOutputAssetId, firstOutputAssetName);
        }
        private (string AssetName, Uri AssetUri) GetTupleFor_GetAssetNameAndUriAsync()
        {
            return (firstOutputAssetName, emptyAssetUri);
        }

        private static List<string> GetListFor_GetAssetFilesNames()
        {
            return new List<string>() { "file1.mp4", "file2.vtt" };
        }


        [Fact]
        public async void CopyFileIntoNewAsset_ShouldLogAndThrow_WhenCreateAsyncThrows()
        {
            // Arrange
            var filesToCopy = new Uri[] { new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/bbb_4k_60fps.mp4") };
            var exception = new Exception();
            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateEmptyAssetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act
            string assetIdReturned = string.Empty;
            var exceptionReturned = await Record.ExceptionAsync(async () =>
            {
                assetIdReturned = await _service.CopyFilesIntoNewAsset(filesToCopy).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exceptionReturned.ShouldBeOfType(typeof(GridwichEncodeCreateJobException));
            assetIdReturned.ShouldBeNullOrEmpty();
            Mock.Get(_log).Verify(x =>
                x.LogExceptionObject(LogEventIds.MediaServicesV2InputAssetError, exception, It.IsAny<object>()),
                Times.Once,
                "It should log the exception.");
        }

        [Fact]
        public async void CopyFileIntoNewAsset_ShouldLogAndThrow_WithAnInvalidUri()
        {
            // Arrange
            var filesToCopy = new Uri[] { new Uri("https://itsaninvaliduri.com") };
            var exception = new Exception();
            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateEmptyAssetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act
            string assetIdReturned = string.Empty;
            var exceptionReturned = await Record.ExceptionAsync(async () =>
            {
                assetIdReturned = await _service.CopyFilesIntoNewAsset(filesToCopy).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exceptionReturned.ShouldBeOfType(typeof(GridwichEncodeCreateJobException));
            assetIdReturned.ShouldBeNullOrEmpty();
            Mock.Get(_log).Verify(x =>
                x.LogExceptionObject(LogEventIds.MediaServicesV2InputAssetError, exception, It.IsAny<object>()),
                Times.Once,
                "It should log the exception.");
        }



        [Fact]
        public async void CopyFileIntoNewAsset_ShouldReturnAssetId_InHappyPath()
        {
            // Arrange
            var filesToCopy = new Uri[] { new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/bbb_4k_60fps.mp4") };
            var copiedEvent = new Mock<CopyFromUriOperation>();
            var blobProperties = new Mock<BlobProperties>();
            var emptyAssetInfo = GetTupleFor_CreateEmptyAssetAsync();

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateEmptyAssetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptyAssetInfo);

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateFileInfosAsync(It.IsAny<string>()));

            Mock.Get(_storageService)
                .Setup(ss => ss.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
            Mock.Get(_storageService)
                .Setup(ss => ss.BlobCopy(filesToCopy.First(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(copiedEvent.Object);
            Mock.Get(_storageService)
                .Setup(ss => ss.GetBlobPropertiesAsync(filesToCopy.First(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobProperties.Object);

            // Act
            var result = await _service.CopyFilesIntoNewAsset(filesToCopy).ConfigureAwait(true);

            // Assert
            result.ShouldBe(emptyAssetInfo.AssetId, "Should return the asset id created by the wrapper.");
            Mock.Get(_log).Verify(x => x.LogExceptionObject(It.IsAny<EventId>(),
                It.IsAny<Exception>(), It.IsAny<Uri>()), Times.Never,
                "No exceptions should have been logged.");
        }

        [Fact]
        public async void CopyFileIntoNewAsset_ShouldReturnAssetId_InHappyPathWithFolder()
        {
            // Arrange
            var filesToCopy = new Uri[] { new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/subfolder/bbb_4k_60fps.mp4") };
            var copiedEvent = new Mock<CopyFromUriOperation>();
            var blobProperties = new Mock<BlobProperties>();
            var emptyAssetInfo = GetTupleFor_CreateEmptyAssetAsync();

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateEmptyAssetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptyAssetInfo);

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateFileInfosAsync(It.IsAny<string>()));

            Mock.Get(_storageService)
                .Setup(ss => ss.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
            Mock.Get(_storageService)
                .Setup(ss => ss.BlobCopy(filesToCopy.First(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(copiedEvent.Object);
            Mock.Get(_storageService)
                .Setup(ss => ss.GetBlobPropertiesAsync(filesToCopy.First(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobProperties.Object);

            // Act
            var result = await _service.CopyFilesIntoNewAsset(filesToCopy).ConfigureAwait(true);

            // Assert
            result.ShouldBe(emptyAssetInfo.AssetId, "Should return the asset id created by the wrapper.");
            Mock.Get(_log).Verify(x => x.LogExceptionObject(It.IsAny<EventId>(),
                It.IsAny<Exception>(), It.IsAny<Uri>()), Times.Never,
                "No exceptions should have been logged.");
        }

        [Fact]
        public async void CopyFileIntoNewAsset_ShouldLogAndThrow_WhenBlobCopyThrows()
        {
            // Arrange
            var filesToCopy = new Uri[] { new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/bbb_4k_60fps.mp4") };
            var storageException = new Mock<Exception>();
            var emptyAssetInfo = GetTupleFor_CreateEmptyAssetAsync();

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateEmptyAssetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptyAssetInfo);
            Mock.Get(_storageService)
                .Setup(ss => ss.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
            Mock.Get(_storageService)
                .Setup(ss => ss.BlobCopy(filesToCopy.First(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(storageException.Object);

            // Act
            string assetIdReturned = string.Empty;
            var exceptionReturned = await Record.ExceptionAsync(async () =>
            {
                assetIdReturned = await _service.CopyFilesIntoNewAsset(filesToCopy).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exceptionReturned.ShouldBeOfType(typeof(GridwichEncodeCreateJobException));
            assetIdReturned.ShouldBeNullOrEmpty();
            Mock.Get(_log).Verify(x => x.LogExceptionObject(LogEventIds.MediaServicesV2CopyFileAndUpdateAssetError,
                storageException.Object, It.IsAny<object>()), Times.Once,
                "The exception should be logged.");
        }

        [Fact]
        public async void CopyFileIntoNewAsset_ShouldLogAndThrow_WhenBlobDoesNotExist()
        {
            // Arrange
            var filesToCopy = new Uri[] { new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/404blob.mp4") };
            var emptyAssetInfo = GetTupleFor_CreateEmptyAssetAsync();

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateEmptyAssetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emptyAssetInfo);
            Mock.Get(_storageService)
                .Setup(ss => ss.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(false);

            // Act
            string assetIdReturned = string.Empty;
            var exceptionReturned = await Record.ExceptionAsync(async () =>
            {
                assetIdReturned = await _service.CopyFilesIntoNewAsset(filesToCopy).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exceptionReturned.ShouldBeOfType(typeof(GridwichMediaServicesV2Exception));
            assetIdReturned.ShouldBeNullOrEmpty();
            Mock.Get(_log).Verify(x => x.LogEventObject(LogEventIds.MediaServicesV2AttemptToUseNonexistentBlobAsInput,
                It.IsAny<object>()), Times.Once,
                "The exception should be logged.");
        }

        [Fact]
        public async void CopyOutputAssetToOutputContainerAsync_ShouldLogAndThrow_WhenBlobCopyThrows()
        {
            // Arrange
            var fileToCopy = new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/bbb_4k_60fps.mp4");
            var storageException = new Mock<Exception>();

            var jobId = "nb:jid:UUID:ff4df607-1111-42f0-bc17-a481b1331e56";

            var opContext = JObject.Parse("{\"key\": \"value\"}");
            var correlationData = new Dictionary<string, string>
            {
                { "outputAssetContainer", "https://gridwichsprites00sasb.blob.core.windows.net/somelowercasecontainer/" },
                { "operationContext", opContext.ToString() },
            };
            var encodedData = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(correlationData));
            var expectedTask = GetTupleFor_GetFirstTaskAsync(encodedData);
            var expectedFirstOutputAsset = GetTupleFor_GetFirstOutputAssetAsync();
            var expectedAssetNameAndUri = GetTupleFor_GetAssetNameAndUriAsync();
            var expectedAssetFilesNames = GetListFor_GetAssetFilesNames();

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetFirstTaskAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedTask);

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetFirstOutputAssetAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedFirstOutputAsset);

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetAssetNameAndUriAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedAssetNameAndUri);

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetAssetFilesNames(It.IsAny<string>()))
                .ReturnsAsync(expectedAssetFilesNames);

            Mock.Get(_storageService)
                .Setup(ss => ss.BlobCopy(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(storageException.Object);

            // Act
            Uri[] urisReturned = null;
            var exceptionReturned = await Record.ExceptionAsync(async () =>
            {
                urisReturned = await _service.CopyOutputAssetToOutputContainerAsync(jobId).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exceptionReturned.ShouldBeOfType(typeof(GridwichEncodeCreateJobException));
            urisReturned.ShouldBeNull();
            Mock.Get(_log).Verify(x => x.LogExceptionObject(LogEventIds.MediaServicesV2CopyOutputAssetError,
                storageException.Object, It.IsAny<object>()), Times.Once,
                "The exception should be logged.");
        }

        [Fact]
        public async void SubmitMesJob_ShouldReturnTrue_InHappyPath()
        {
            // Arrange
            var jobId = "nb:jid:UUID:ff4df607-1111-42f0-bc17-a481b1331e56";
            var processorId = "nb:mpid:UUID:ff4df607-d419-42f0-bc17-a481b1331e56";
            var notificationEndpointId = "nb:neid:UUID:ff4df607-2222-42f0-bc17-a481b1331e56";
            var inputAssetId = "assetId";
            var preset = "trustmeitsapreset";
            var outputContainer = new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/bbb_4k_60fps.mp4");
            var callbackEndpoint = new Uri("https://callback.com");
            var correlationData = new Dictionary<string, string>
            {
                { "somekey", "somevalue" },
                { "another", "value" },
            };

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetLatestMediaProcessorAsync(It.IsAny<string>()))
                .ReturnsAsync(processorId);
            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetOrCreateNotificationEndPointAsync(It.IsAny<string>(), It.IsAny<Uri>()))
                .ReturnsAsync(notificationEndpointId);
            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(jobId);

            // Act
            var result = await _service.SubmitMesJobAsync(inputAssetId, preset, outputContainer, callbackEndpoint, correlationData).ConfigureAwait(false);

            // Assert
            result.ShouldBeEquivalentTo(jobId);
            Mock.Get(_log).Verify(x => x.LogExceptionObject(
                It.IsAny<EventId>(),
                It.IsAny<Exception>(),
                It.IsAny<Uri>()),
                Times.Never, "No exceptions should have been logged.");
        }

        [Fact]
        public async void SubmitMesJob_ShouldFail_WhenCreateJobThrows()
        {
            // Arrange
            var processorId = "nb:mpid:UUID:ff4df607-d419-42f0-bc17-a481b1331e56";
            var notificationEndpointId = "nb:neid:UUID:ff4df607-2222-42f0-bc17-a481b1331e56";
            var inputAssetId = "assetId";
            var preset = "trustmeitsapreset";
            var outputContainer = new Uri("https://gridwichinbox00sahl799.blob.core.windows.net/inbox/bbb_4k_60fps.mp4");
            var callbackEndpoint = new Uri("https://callback.com");
            var correlationData = new Dictionary<string, string>
            {
                { "somekey", "somevalue" },
                { "another", "value" },
            };

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetLatestMediaProcessorAsync(It.IsAny<string>()))
                .ReturnsAsync(processorId);
            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetOrCreateNotificationEndPointAsync(It.IsAny<string>(), It.IsAny<Uri>()))
                .ReturnsAsync(notificationEndpointId);
            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.CreateJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Throws(new Exception());

            // Act
            string jobIdReturned = string.Empty;
            var exceptionReturned = await Record.ExceptionAsync(async () =>
            {
                jobIdReturned = await _service.SubmitMesJobAsync(
                                    inputAssetId,
                                    preset,
                                    outputContainer,
                                    callbackEndpoint,
                                    correlationData).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exceptionReturned.ShouldBeOfType(typeof(GridwichEncodeCreateJobException));
            Mock.Get(_log).Verify(x => x.LogExceptionObject(
                LogEventIds.MediaServicesV2SubmitMesJobFailure,
                It.IsAny<Exception>(),
                It.IsAny<object>()),
                Times.Once, "The exception should be logged.");
        }

        [Fact]
        public async Task GetOpContextForJob_Succeeds_InHappyPathAsync()
        {
            // Arrange
            var jobId = "jobId";
            var opContext = JObject.Parse("{\"key\": \"value\"}");
            var correlationData = new Dictionary<string, string>
            {
                { "operationContext", opContext.ToString() }
            };
            var encodedData = Base64UrlEncoder.Encode(JsonConvert.SerializeObject(correlationData));
            var emptyTaskInfo = GetTupleFor_GetFirstTaskAsync(encodedData);

            Mock.Get(_mediaServicesV2RestWrapper)
                .Setup(x => x.GetFirstTaskAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyTaskInfo);

            // Act
            var result = await _service.GetOperationContextForJobAsync(jobId).ConfigureAwait(false);

            // Assert
            result.ShouldBeEquivalentTo(opContext);
            Mock.Get(_log).Verify(x => x.LogExceptionObject(
                It.IsAny<EventId>(),
                It.IsAny<Exception>(),
                It.IsAny<Uri>()),
                Times.Never, "No exceptions should have been logged.");
        }
    }
}
