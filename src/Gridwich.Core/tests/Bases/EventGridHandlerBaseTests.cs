using System;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;

using Xunit;

namespace Gridwich.CoreTests.Bases
{
    public class EventGridHandlerBaseTests
    {
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        private readonly DummyStorageEventHandler _handler;
        private readonly IObjectLogger<DummyStorageEventHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;

        public EventGridHandlerBaseTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<DummyStorageEventHandler>>();
            _handler = new DummyStorageEventHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "654BCC8B-B61C-4764-B536-541AF3779818";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(CustomEventTypes.RequestBlobCopy, "1.0")]
        [InlineData(CustomEventTypes.RequestBlobCopy, "2.0")]
        [InlineData(CustomEventTypes.RequestBlobCopy, "NotAnInt")]
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
        [InlineData(EventTypes.StorageBlobDeletedEvent, "1.0")]
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
            var blobUri = new Uri(_expectedInboxUrl);
            JObject operationContext = JObject.Parse("{}");
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobMetadataCreateDTO { BlobUri = blobUri, OperationContext = JObject.FromObject(operationContext) })
            };

            var blobBaseClient = new BlobBaseClient(blobUri);
            var blobProperties = new BlobProperties();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(blobUri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(new JObject() { });
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "No exceptions should be logged when all succeeds");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(It.Is<EventId>(e => e.Id >= LogEventIds.FailedToDeserializeEventData.Id), It.IsAny<object>()),
                Times.Never,
                "No events with IDs greater than an error ID (could be any LogEventId here) should be logged");
            Mock.Get(_storageService).Verify(x =>
                x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to get blob metadata.");
            Mock.Get(_eventGridPublisher).Verify(x =>
                x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Between(2, 2, Moq.Range.Inclusive),
                "Should only publish acknowledgement and output events");
        }

        [Fact]
        public async Task HandleAsync_ShouldAcknowledgeEvent_WhenHandlingRequestorEvent()
        {
            // Arrange
            var blobUri = new Uri(_expectedInboxUrl);
            JObject operationContext = JObject.Parse("{}");
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobMetadataCreateDTO { BlobUri = blobUri, OperationContext = JObject.FromObject(operationContext) })
            };
            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(blobUri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(new JObject() { });

            var expectedEventToPublishCaptureMatch = new CaptureMatch<EventGridEvent>(x =>
            {
                // Assert values in the object passed to the publisher:
                x.EventType.ShouldBe("gridwich.acknowledge");
                x.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            });

            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(Capture.With(expectedEventToPublishCaptureMatch)))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.StartingAcknowledgement, It.IsAny<object>()),
                Times.Once,
                "It should log that it is starting the acknowledgement");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.FinishedAcknowledgement, It.IsAny<object>()),
                Times.Once,
                "It should log that it finished the acknowledgement");
        }

        [Fact]
        public async Task HandleAsync_ShouldNotAcknowledgEvent_WhenHandlingNonRequestorEvent()
        {
            // Arrange
            var blobUri = new Uri(_expectedInboxUrl);
            JObject operationContext = JObject.Parse("{}");
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = "Not External Request Event",  // This MUST match the string in the constructor of DummyStorageEventHandler.cs
                DataVersion = "1.0",
                Data = string.Empty,
            };

            // Act
            bool handleAsyncResult;
            try
            {
                handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);
            }
            catch (System.ArgumentException)
            {
                // nothing to do.
            }
            catch (System.Exception)
            {
                // not just the usual argument exception due to the request being
                // an unknown event.
                throw;
            }

            // Assert
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.StartingAcknowledgement, It.IsAny<object>()),
                Times.Never,
                "It should not log that it is starting the acknowledgement");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.FinishedAcknowledgement, It.IsAny<object>()),
                Times.Never,
                "It should not log that it finished the acknowledgement");
            Mock.Get(_eventGridPublisher).Verify(x =>
                x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Once,
                "It should only publish the output event");
        }


        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenTriesToHandleNonCreateMetadataEventType()
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
        public async Task HandleAsync_ShouldReturnFalseAndLogEvent_WhenGetMetadataThrows()
        {
            // Arrange
            var blobUri = new Uri(_expectedInboxUrl);
            JObject operationContext = JObject.Parse("{\"something\": \"something value\"}");

            var topicEndpointUri = new Uri("https://www.topichost.com");
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobCopyDTO { SourceUri = blobUri, DestinationUri = blobUri, OperationContext = JObject.FromObject(operationContext) })
            };
            var blobBaseClient = new BlobBaseClient(blobUri);
            var blobProperties = new BlobProperties();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(new Exception());
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(
                    It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);
            Mock.Get(_logger)
                .Setup(x => x.LogExceptionObject(
                    out appInsightsUri,
                    LogEventIds.GridwichUnhandledException,
                    It.IsAny<Exception>(),
                    It.IsAny<object>()));

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false);

            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(out appInsightsUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the work fails");
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnFalseAndLogEvents_WhenPublishingThrows()
        {
            // Arrange
            var blobUri = new Uri(_expectedInboxUrl);
            JObject operationContext = JObject.Parse("{\"something\": \"something value\"}");
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobCopyDTO { SourceUri = blobUri, DestinationUri = blobUri, OperationContext = JObject.FromObject(operationContext) })
            };
            var blobBaseClient = new BlobBaseClient(blobUri);
            var blobProperties = new BlobProperties();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(blobUri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(new JObject());
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ThrowsAsync(new InvalidOperationException());

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false);
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.FailedCriticallyToPublishEvent,
                    It.IsAny<Exception>(), It.IsAny<object>()),
                    Times.AtLeastOnce,
                    "An exception should be logged when the publishing fails");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.FailedToAcknowledge,
                It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the acknowledgement fails");
        }

        [Fact]
        public void HandlesEvent_ShouldReturnEventGridEventWithoutEventId_WhenReleventParameterIsFalse()
        {
            // Arrange
            string eventType = "TEST";
            string subject = "TEST_SUBJECT";
            ResponseBaseDTO dto = new ResponseAcknowledgeDTO(eventType);

            // Act
            var result = _handler.GetEventGridEventFromResponseDTO(dto, subject, false);

            // Assert
            result.Subject.ShouldBe(subject);
        }

        [Fact]
        public void HandlesEvent_ShouldReturnEventGridEventWithoutEventId_WhenReleventParameterIsTrue()
        {
            // Arrange
            string eventType = "TEST";
            string subject = "TEST_SUBJECT";
            ResponseBaseDTO dto = new ResponseAcknowledgeDTO(eventType);

            // Act
            var result = _handler.GetEventGridEventFromResponseDTO(dto, subject);

            // Assert
            var subjectParts = result.Subject.Split("/");
            subjectParts[0].ShouldBe(subject);
            Guid.TryParse(subjectParts[1], out _).ShouldBe(true);
        }

        [Fact]
        public async Task HandleAsyncShouldReturnFalseAndLogWithBadData()
        {
            // Arrange
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = "Bad Data"
            };

            // Arrange Mocks
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(false, "should return false with bad data in eventGrid");
            Uri locatorUri;
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(out locatorUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.AtLeastOnce,
                "An exception should be logged when the event Data field has bad data");
        }
    }
}