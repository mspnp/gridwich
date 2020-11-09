using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Shouldly;

using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class BlobCopyHandlerTests
    {
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        private readonly BlobCopyHandler _handler;
        private readonly IObjectLogger<BlobCopyHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;
        public BlobCopyHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobCopyHandler>>();
            _handler = new BlobCopyHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "C5BC453D-58CF-4F16-A2FF-16647F6CBF81";

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
        [InlineData(CustomEventTypes.RequestBlobAnalysisCreate, "1.0")]
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
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var sourceUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/elephantsDream_hd.mp4");
            var destinationUri = new Uri("https://gridwichlts00sasb.blob.core.windows.net/test00/elephantsDream_hd-copy.mp4");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobCopyDTO
                {
                    SourceUri = sourceUri,
                    DestinationUri = destinationUri,
                    OperationContext = JObject.Parse("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}")
                }),
            };
            var metadata = JObject.Parse("{\"prop\":\"value\"}");

            EventGridEvent publishedEvent = null;
            var copiedEvent = new Mock<CopyFromUriOperation>();
            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.BlobCopy(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(copiedEvent.Object);
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(sourceUri, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(metadata);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.EventNotSupported, It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds");
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds");
            Mock.Get(_storageService).Verify(x =>
                x.BlobCopy(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to get blob metadata.");
            Mock.Get(_eventGridPublisher)
                .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Between(2, 2, Moq.Range.Inclusive),
                "Should publich acknowledge and output events");

            // Assert publishedEvent:
            publishedEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobCopyScheduled);
            publishedEvent.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobCopyScheduledDTO));
            var data = (ResponseBlobCopyScheduledDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someProp1").ShouldBeTrue();
            data.OperationContext["someProp1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someProp2").ShouldBeTrue();
            data.OperationContext["someProp2"].ShouldBe("someValue2");
            data.SourceUri.ToString().ShouldBe(sourceUri.ToString());
            data.DestinationUri.ToString().ShouldBe(destinationUri.ToString());
            data.BlobMetadata["prop"].ShouldBe("value");
        }

        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenTriesToHandleNonRequestorBlobCopyEventType()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.MapsGeofenceEnteredEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobCreatedEventData { Url = _expectedInboxUrl })
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _handler.HandleAsync(testEvent).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.EventNotSupported,
                    It.IsAny<object>()),
                Times.Once,
                $"Should log {nameof(LogEventIds.EventNotSupported)}");
        }

        [Fact]
        public async Task HandleAsync_ShouldFail_WhenEventDataIsNull()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = string.Empty,
            };
            EventGridEvent resultEvent = null;
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => resultEvent = eventGridEvent)
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
            resultEvent.ShouldNotBeNull();
            resultEvent.EventType.ShouldBe(CustomEventTypes.ResponseFailure);
        }

        [Theory]
        [InlineData("{ \"SourceUri\":\"Not_a_uri\", \"DestinationUri\":\"https://account.blob.core.windows.net/container/file.type\"}")]
        [InlineData("{ \"SourceUri\":\"https://account.blob.core.windows.net/container/file.type\", \"DestinationUri\":\"Not_a_uri\"}")]
        [InlineData("{ \"SourceUri\":\"Not_a_uri\", \"DestinationUri\":\"Not_a_uri\"}")]
        public async Task HandleAsync_ShouldFail_WhenReportedBlobUriIsNotUri(string data)
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JObject.Parse(data)
            };
            EventGridEvent resultEvent = null;
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => resultEvent = eventGridEvent)
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
            resultEvent.ShouldNotBeNull();
            resultEvent.EventType.ShouldBe(CustomEventTypes.ResponseFailure);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogEvent_WhenPublisherDoesNotPublish()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var sourceUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/elephantsDream_hd.mp4");
            var destinationUri = new Uri("https://gridwichlts00sasb.blob.core.windows.net/test00/elephantsDream_hd-copy.mp4");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobCopyDTO
                {
                    SourceUri = sourceUri,
                    DestinationUri = destinationUri,
                    OperationContext = JObject.Parse("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}")
                }),
            };
            EventGridEvent publishedEvent = null;
            var copiedEvent = new Mock<CopyFromUriOperation>();
            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.BlobCopy(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(copiedEvent.Object);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(false);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false);
            Mock.Get(_logger).Verify(x =>
                 x.LogEventObject(LogEventIds.FailedToPublishEvent,
                     It.IsAny<object>()),
                 Times.Between(2, 2, Moq.Range.Inclusive),
                 $"Should log {nameof(LogEventIds.FailedToPublishEvent)}");
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnFalseAndLogEvent_WhenPublishingThrows()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var sourceUri = new Uri("https://gridwichinbox00sasb.blob.core.windows.net/test00/elephantsDream_hd.mp4");
            var destinationUri = new Uri("https://gridwichlts00sasb.blob.core.windows.net/test00/elephantsDream_hd-copy.mp4");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobCopy,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobCopyDTO
                {
                    SourceUri = sourceUri,
                    DestinationUri = destinationUri,
                    OperationContext = JObject.Parse("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}")
                }),
            };
            // EventGridEvent publishedEvent = null;
            var copiedEvent = new Mock<CopyFromUriOperation>();
            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.BlobCopy(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(copiedEvent.Object);
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
        }
    }
}