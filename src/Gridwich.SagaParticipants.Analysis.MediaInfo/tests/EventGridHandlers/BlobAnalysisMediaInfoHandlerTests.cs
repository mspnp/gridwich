using System;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Analysis.MediaInfo.EventGridHandlers;
using Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions;
using Gridwich.SagaParticipants.Analysis.MediaInfo.MediaInfoProviders;

using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;

using Xunit;

namespace Gridwich.SagaParticipants.Analysis.MediaInfoTests.EventGridHandlers
{
    public class BlobAnalysisMediaInfoHandlerTests
    {
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        // private const string _expectedMd5 = "1000";
        private const string _expectedOperationContext = "{\"foo\":\"bar\",\"one\":\"two\"}";
        private const string _expectedAnalyzerSpecificData = "{\"mediaInfo\": {\"commandLineOptions\":{\"Complete\":\"1\", \"Output\":\"JSON\"} } }";
        private readonly BlobAnalysisMediaInfoHandler _handler;
        private readonly IObjectLogger<BlobAnalysisMediaInfoHandler> _logger;
        private readonly IStorageService _storageService;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly IMediaInfoReportService _mediaInfoReportService;
        public BlobAnalysisMediaInfoHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _mediaInfoReportService = Mock.Of<IMediaInfoReportService>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobAnalysisMediaInfoHandler>>();
            _handler = new BlobAnalysisMediaInfoHandler(
                _logger,
                _storageService,
                _eventGridPublisher,
                _mediaInfoReportService);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "A7250940-98C8-4CC5-A66C-45A2972FF3A2";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(CustomEventTypes.RequestBlobAnalysisCreate, "1.0")]
        [InlineData(CustomEventTypes.RequestBlobAnalysisCreate, "2.0")]
        [InlineData(CustomEventTypes.RequestBlobAnalysisCreate, "NotAnInt")]
        public void HandlesEvent_ShouldHandle_EventType_and_Version(string eventType, string dataVersion)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = _handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBeTrue();
        }

        [Theory]
        [InlineData(EventTypes.StorageBlobCreatedEvent, "1.0")]
        [InlineData("NotAnExpectedEventType", "1.0")]
        [InlineData("NotAnExpectedEventType", "NotAnInt")]
        public void HandlesEvent_ShouldNotHandle_EventType_and_Version(string eventType, string dataVersion)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = _handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBeFalse();
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnTrueAndNotLog_WhenNoErrors()
        {
            // Arrange
            var data = new RequestBlobAnalysisCreateDTO()
            {
                AnalyzerSpecificData = JsonHelpers.JsonToJObject(_expectedAnalyzerSpecificData, true),
                BlobUri = new Uri(_expectedInboxUrl),
                OperationContext = JsonHelpers.DeserializeOperationContext(_expectedOperationContext),
            };
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobAnalysisCreate,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(data)
            };
            const string connectionString = "CONNECTION_STRING";
            JObject report = JsonHelpers.JsonToJObject("{}", true);
            JObject metadata = JsonHelpers.JsonToJObject("{}", true);
            EventGridEvent resultEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetConnectionStringForAccount(It.IsAny<string>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(connectionString);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => resultEvent = eventGridEvent)
                .ReturnsAsync(true);
            Mock.Get(_mediaInfoReportService)
                .Setup(x => x.GetMediaInfoCompleteInformForUriAsync(data.BlobUri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(report);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(data.BlobUri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(metadata);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert positive results
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");

            resultEvent.ShouldNotBeNull();
            resultEvent.Data.ShouldNotBeNull();
            resultEvent.Data.ShouldBeOfType(typeof(ResponseBlobAnalysisSuccessDTO));

            ResponseBlobAnalysisSuccessDTO eventData = (ResponseBlobAnalysisSuccessDTO)resultEvent.Data;
            eventData.AnalysisResult.ShouldNotBeNull();
            eventData.BlobMetadata.ShouldNotBeNull();
            eventData.BlobUri.ToString().ShouldBe(_expectedInboxUrl);
            // eventData.Md5.ShouldBe(_expectedMd5);  // TODO
            eventData.AnalysisResult.ShouldBe(report);

            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.StartingEventHandling, It.IsAny<object>()),
                Times.Once,
                "An accepted event type should log information when it is about to begin");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.AboutToCallAnalysisDeliveryEntry, It.IsAny<object>()),
                Times.Once,
                "An accepted event type should log information when it is about to begin analysis");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.AnalysisOfDeliveryFileSuccessful, It.IsAny<object>()),
                Times.Once,
                "An accepted event type should log information when analysis is successful");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.FinishedEventHandling, It.IsAny<object>()),
                Times.Once,
                "An accepted event type should log information when the event handling is complete");
            // Assert negative results
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.EventNotSupported, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds");
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.FailedToPublishEvent, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds");
        }

        [Fact]
        public async Task HandleAsync_ShouldHandleReportGenerationResult_WhenReportGenerationFails()
        {
            // Arrange
            var fileUri = new Uri(_expectedInboxUrl);
            var data = new RequestBlobAnalysisCreateDTO()
            {
                AnalyzerSpecificData = JsonHelpers.JsonToJObject(_expectedAnalyzerSpecificData, true),
                BlobUri = new Uri(_expectedInboxUrl),
                OperationContext = JsonHelpers.DeserializeOperationContext(_expectedOperationContext),
            };
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobAnalysisCreate,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(data)
            };
            var storageContext = new StorageClientProviderContext(_expectedOperationContext);

            const string connectionString = "CONNECTION_STRING";
            EventGridEvent resultEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetConnectionStringForAccount(It.IsAny<string>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(connectionString);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => resultEvent = eventGridEvent)
                .ReturnsAsync(true);
            // Note the exact LogEventId does not matter
            Mock.Get(_mediaInfoReportService)
                .Setup(x => x.GetMediaInfoCompleteInformForUriAsync(fileUri, It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(new GridwichMediaInfoLibException("Error", LogEventIds.MediaInfoLibOpenBufferInitFailed, storageContext.ClientRequestIdAsJObject));
            JObject blobMetadata = null;
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobMetadata);
            Mock.Get(_logger)
                .Setup(x => x.LogEvent(
                    out appInsightsUri,
                    LogEventIds.AnalysisOfDeliveryFileFailed.Id,
                    It.IsAny<string>()));
            Mock.Get(_logger)
                .Setup(x => x.LogExceptionObject(
                    out appInsightsUri,
                    LogEventIds.FailedCriticallyToPublishEvent,
                    It.IsAny<GridwichMediaInfoLibException>(),
                    It.IsAny<object>()));

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert positive results
            handleAsyncResult.ShouldBe(false, "handleAsync should still make an event on failure");

            resultEvent.ShouldNotBeNull();
            resultEvent.Data.ShouldNotBeNull();
            resultEvent.Data.ShouldBeOfType(typeof(ResponseFailureDTO));

            var eventData = (ResponseFailureDTO)resultEvent.Data;
            eventData.HandlerId.ShouldBe("A7250940-98C8-4CC5-A66C-45A2972FF3A2");
            eventData.LogEventMessage.ShouldNotBeNullOrEmpty();
            eventData.EventHandlerClassName.ShouldNotBeNullOrEmpty();
            eventData.OperationContext.ShouldBe(JsonHelpers.JsonToJObject(_expectedOperationContext, true));

            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(out appInsightsUri, It.IsAny<EventId>(),
                        It.IsAny<GridwichMediaInfoLibException>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when report generation fails");
        }

        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenTriesToHandleNonRequestorBlobAnalysisCreatedEventType()
        {
            // Arrange
            const string BLOB_ETAG = "ETAG";
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.MapsGeofenceEnteredEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobCreatedEventData { ETag = BLOB_ETAG, Url = BLOB_URL })
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _handler.HandleAsync(testEvent).ConfigureAwait(true)).ConfigureAwait(true);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnFalseAndLogEvent_WhenPublishingThrows()
        {
            // Arrange
            var data = new RequestBlobAnalysisCreateDTO()
            {
                AnalyzerSpecificData = JsonHelpers.JsonToJObject(_expectedAnalyzerSpecificData, true),
                BlobUri = new Uri(_expectedInboxUrl),
                OperationContext = JsonHelpers.DeserializeOperationContext(_expectedOperationContext),
            };
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobAnalysisCreate,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(data)
            };
            JObject report = JsonHelpers.JsonToJObject("{}", true);

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            JObject blobMetadata = null;
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobMetadata);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ThrowsAsync(new InvalidOperationException());
            Mock.Get(_mediaInfoReportService)
                      .Setup(x => x.GetMediaInfoCompleteInformForUriAsync(data.BlobUri, It.IsAny<StorageClientProviderContext>()))
                      .ReturnsAsync(report);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false);
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.FailedCriticallyToPublishEvent,
                    It.IsAny<InvalidOperationException>(), It.IsAny<object>()),
                Times.AtLeastOnce,
                "An exception should be logged when the publishing fails");
        }
    }
}