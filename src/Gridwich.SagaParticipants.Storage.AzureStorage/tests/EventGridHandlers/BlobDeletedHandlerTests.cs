using System;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;

using Newtonsoft.Json;

using Shouldly;

using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class BlobDeletedHandlerTests
    {
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        private readonly BlobDeletedHandler _handler;
        private readonly IObjectLogger<BlobDeletedHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IEventGridPublisher _eventGridPublisher;
        public BlobDeletedHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobDeletedHandler>>();
            _handler = new BlobDeletedHandler(
                _logger,
                _eventGridPublisher);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "ed59019b-5e8e-43a7-8b83-490611cdb722";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId, StringCompareShould.IgnoreCase);
        }

        [Theory]
        [InlineData(EventTypes.StorageBlobDeletedEvent, "1.0")]
        [InlineData(EventTypes.StorageBlobDeletedEvent, "2.0")]
        [InlineData(EventTypes.StorageBlobDeletedEvent, "NotAnInt")]
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
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = EventTypes.StorageBlobDeletedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobDeletedEventData
                {
                    Url = _expectedInboxUrl,
                    ClientRequestId = "{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}",
                }),
            };
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds");
            Mock.Get(_eventGridPublisher).Verify(x =>
                x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Once,
                "Should only publish one event.");

            // Assert publishedEvent:
            publishedEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobDeleteSuccess);
            publishedEvent.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobDeleteSuccessDTO));
            var data = (ResponseBlobDeleteSuccessDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
            data.BlobUri.ToString().ShouldBe(_expectedInboxUrl);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void HandleAsync_ShouldReturnNullOperationContext_WhenItIsAGuid(bool muteContext)
        {
            // Arrange
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var reqId = new StorageClientProviderContext("9d87e668-0000-0000-0000-b84ff6a53784");
            reqId.IsMuted = muteContext;

            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = EventTypes.StorageBlobDeletedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobDeletedEventData
                {
                    Url = BLOB_URL,
                    ClientRequestId = reqId.ClientRequestID,
                })
            };
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");

            // Assert publishedEvent:
            if (muteContext)
            {
                Mock.Get(_eventGridPublisher).Verify(x =>
                    x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                    Times.Never,
                    "Muted context results should not be published.");
                publishedEvent.ShouldBeNull();
            }
            else
            {
                publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobDeleteSuccessDTO));
                var data = (ResponseBlobDeleteSuccessDTO)publishedEvent.Data;
                data.OperationContext.ShouldNotBeNull();
                data.OperationContext.ShouldBeEquivalentTo(reqId.ClientRequestIdAsJObject);
            }
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnOperationContext_WhenItIsWellFormed()
        {
            // Arrange
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = EventTypes.StorageBlobDeletedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobDeletedEventData
                {
                    Url = BLOB_URL,
                    ClientRequestId = "{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}",
                })
            };
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");

            // Assert publishedEvent:
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobDeleteSuccessDTO));
            var data = (ResponseBlobDeleteSuccessDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
        }

        [Fact]
        public async Task HandleAsync_ShouldLogJsonReaderException_WhenClientRequestIdIsNotWellFormed()
        {
            // Arrange
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = EventTypes.StorageBlobDeletedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobDeletedEventData
                {
                    Url = BLOB_URL,
                    ClientRequestId = "{\"someIdType1\":\"someValue1\", \"someIdType2\":\"truncatedJsonHere",
                })
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false, "handleAsync should have returned false due to clientRequestID being malformed");

            // Assert publishedEvent:
            Uri appInsightUri;
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException,
                    It.IsAny<Exception>(),
                    It.IsAny<object>()),
                Times.Once,
                $"Should log {nameof(LogEventIds.GridwichUnhandledException)}");
        }

        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenTriesToHandleNonStorageBlobDeletedEventType()
        {
            // Arrange
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.MapsGeofenceEnteredEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobDeletedEventData { Url = BLOB_URL })
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _handler.HandleAsync(testEvent).ConfigureAwait(true)).ConfigureAwait(true);
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.EventNotSupported,
                    It.IsAny<object>()),
                Times.Once,
                $"Should log {nameof(LogEventIds.EventNotSupported)}");
        }

        [Fact]
        public async Task HandleAsync_ShouldFail_WhenReportedBlobUriIsNotUri()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.StorageBlobDeletedEvent,
                DataVersion = "1.0",
                Data = JsonHelpers.JsonToJObject("{ \"Url\":\"Not_a_uri\" }", true),
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false);
            Uri appInsightUri;
            Mock.Get(_logger).Verify(x =>
                 x.LogExceptionObject(out appInsightUri, LogEventIds.FailedToCreateBlobDeletedDataWithEventDataInBlobDeletedHandler,
                     It.IsAny<Exception>(), It.IsAny<object>()),
                 Times.Once,
                 $"Should log {nameof(LogEventIds.FailedToCreateBlobDeletedDataWithEventDataInBlobDeletedHandler)}");
        }
    }
}
