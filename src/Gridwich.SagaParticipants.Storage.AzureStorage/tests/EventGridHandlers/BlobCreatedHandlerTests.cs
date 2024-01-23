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
using System;
using System.Threading.Tasks;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class BlobCreatedHandlerTests
    {
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        private readonly BlobCreatedHandler _handler;
        private readonly IObjectLogger<BlobCreatedHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;
        public BlobCreatedHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobCreatedHandler>>();
            _handler = new BlobCreatedHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "9d87e668-8d8c-4dd7-a7e2-b84ff6a53784";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(EventTypes.StorageBlobCreatedEvent, "1.0")]
        [InlineData(EventTypes.StorageBlobCreatedEvent, "2.0")]
        [InlineData(EventTypes.StorageBlobCreatedEvent, "NotAnInt")]
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
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = EventTypes.StorageBlobCreatedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobCreatedEventData
                {
                    Url = _expectedInboxUrl,
                    ClientRequestId = "{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}",
                }),
            };
            var expectedBlobMetadata = JsonHelpers.JsonToJObject("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}", true);
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(expectedBlobMetadata);
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
            Mock.Get(_storageService).Verify(x =>
                x.GetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to get blob metadata.");
            Mock.Get(_eventGridPublisher).Verify(x =>
                x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Once,
                "Should only publish one event.");

            // Assert publishedEvent:
            publishedEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobCreatedSuccess);
            publishedEvent.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobCreatedSuccessDTO));
            var data = (ResponseBlobCreatedSuccessDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
            data.BlobUri.ToString().ShouldBe(_expectedInboxUrl);
            data.BlobMetadata.ShouldNotBeNull();
            data.BlobMetadata.ContainsKey("someProp1").ShouldBeTrue();
            data.BlobMetadata["someProp1"].ShouldBe("someValue1");
            data.BlobMetadata.ContainsKey("someProp2").ShouldBeTrue();
            data.BlobMetadata["someProp2"].ShouldBe("someValue2");
        }

        [Theory]
        [InlineData(true, "9d87e668-0000-0000-0000-b84ff6a53784", true, null)]
        [InlineData(false, "9d87e668-0000-0000-0000-b84ff6a53784", true, true)]
        [InlineData(true, "9d87e668-0000-0000-0000-b84ff6a53784", true, false)]
        [InlineData(true, "", true, false)]
        [InlineData(false, "", true, true)]
        [InlineData(false, "   ", true, true)]
        [InlineData(true, "    ", true, false)]
        [InlineData(false, "{\"is\":1}", true, true)]
        [InlineData(true, "{\"not\":1}", true, false)]
        public async Task HandleAsync_ShouldPublishAppropriately_GivenOperationContextsOfGuidsAndBlanks(
            bool shouldBePublished, string contextString, bool doAdjustViaContext, bool? toBeMuted)
        {
            string clientRequestIdForContext = contextString;
            if (doAdjustViaContext)
            {
                // run string through context to get final version for HTTP header
                var t = new StorageClientProviderContext(contextString, muted: toBeMuted);
                clientRequestIdForContext = t.ClientRequestID;
            }

            // Arrange
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = EventTypes.StorageBlobCreatedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobCreatedEventData
                {
                    Url = BLOB_URL,
                    ClientRequestId = clientRequestIdForContext,
                })
            };
            var expectedBlobMetadata = JsonHelpers.JsonToJObject("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}", true);
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(expectedBlobMetadata);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");

            // Assert publishedEvent:

            if (shouldBePublished)
            {
                publishedEvent.ShouldNotBeNull();
                publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobCreatedSuccessDTO));
                var data = (ResponseBlobCreatedSuccessDTO)publishedEvent.Data;
                data.OperationContext.ShouldNotBeNull();
                JsonHelpers.SerializeOperationContext(data.OperationContext).ShouldBe(clientRequestIdForContext);
                Mock.Get(_eventGridPublisher)
                    .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                            Times.AtLeastOnce, "Events should have been published");
            }
            else
            {
                publishedEvent.ShouldBeNull();
                Mock.Get(_eventGridPublisher)
                    .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                            Times.Never, "No events should have been published");
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
                EventType = EventTypes.StorageBlobCreatedEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobCreatedEventData()
                {
                    Url = BLOB_URL,
                    ClientRequestId = "{\"someIdType1\":\"someValue1\", \"someIdType2\":\"someValue2\"}",
                })
            };
            var expectedBlobMetadata = JsonHelpers.JsonToJObject("{\"someProp1\":\"someValue1\", \"someProp2\":\"someValue2\"}", true);
            EventGridEvent publishedEvent = null;

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetBlobMetadataAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(expectedBlobMetadata);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => publishedEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");

            // Assert publishedEvent:
            publishedEvent.Data.ShouldBeOfType(typeof(ResponseBlobCreatedSuccessDTO));
            var data = (ResponseBlobCreatedSuccessDTO)publishedEvent.Data;
            data.ShouldNotBeNull();
            data.OperationContext.ContainsKey("someIdType1").ShouldBeTrue();
            data.OperationContext["someIdType1"].ShouldBe("someValue1");
            data.OperationContext.ContainsKey("someIdType2").ShouldBeTrue();
            data.OperationContext["someIdType2"].ShouldBe("someValue2");
        }

        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenTriesToHandleNonStorageBlobCreatedEventType()
        {
            // Arrange
            const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.MapsGeofenceEnteredEvent,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new StorageBlobCreatedEventData { Url = BLOB_URL })
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
        public async Task HandleAsync_ShouldFail_WhenEventDataIsNull()
        {
            // Arrange
            // const string BLOB_URL = _expectedInboxUrl;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.StorageBlobCreatedEvent,
                DataVersion = "1.0",
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _handler.HandleAsync(testEvent).ConfigureAwait(true)).ConfigureAwait(true);
        }

        [Fact]
        public async Task HandleAsync_ShouldFail_WhenReportedBlobUriIsNotUri()
        {
            // Arrange
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventType = EventTypes.StorageBlobCreatedEvent,
                DataVersion = "1.0",
                Data = JsonHelpers.JsonToJObject("{ \"Url\":\"Not_a_uri\" }", true)
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
                 x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException,
                     It.IsAny<Exception>(), It.IsAny<object>()),
                 Times.Once);
        }
    }
}