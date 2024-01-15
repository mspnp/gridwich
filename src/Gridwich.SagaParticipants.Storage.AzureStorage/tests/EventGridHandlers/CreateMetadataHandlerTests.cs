using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
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
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class CreateMetadataHandlerTests
    {
        private const string _expectedInboxUrl = "https://gridwichinbox01sasb.blob.core.windows.net/10000001-0000-0000-0000-400c0a126fd0/fake_test_asset.mp4";
        private readonly CreateMetadataHandler _handler;
        private readonly IObjectLogger<CreateMetadataHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;
        public CreateMetadataHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<CreateMetadataHandler>>();
            _handler = new CreateMetadataHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "EAB78688-2784-42AF-BC91-6A0E05D4AF6D";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(CustomEventTypes.RequestCreateMetadata, "1.0")]
        [InlineData(CustomEventTypes.RequestCreateMetadata, "2.0")]
        [InlineData(CustomEventTypes.RequestCreateMetadata, "NotAnInt")]
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
            var context = TestHelpers.CreateGUIDContext();
            var metadataData = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestCreateMetadata,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobMetadataCreateDTO
                {
                    BlobUri = blobUri,
                    BlobMetadata = JObject.FromObject(metadataData),
                    OperationContext = context.ClientRequestIdAsJObject,
                }),
            };

            var blobBaseClient = new BlobBaseClient(blobUri);
            var blobProperties = new BlobProperties();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.SetBlobMetadataAsync(blobUri, metadataData, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
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
                "No exceptions should be logged when the publishing succeeds");
            Mock.Get(_logger).Verify(x =>
                x.LogEventObject(LogEventIds.FailedToPublishEvent, It.IsAny<object>()),
                Times.Never,
                "An exception should NOT be logged when the publishing succeeds");
            Mock.Get(_storageService).Verify(x =>
                x.SetBlobMetadataAsync(It.IsAny<Uri>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<StorageClientProviderContext>()),
                Times.Once,
                "Should attempt to set blob metadata.");
            Mock.Get(_eventGridPublisher)
                .Verify(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()),
                Times.Between(2, 2, Moq.Range.Inclusive),
                "Should publish one acknowledge event and the output event.");
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
        public async Task HandleAsync_ShouldReturnFalseAndLogEvent_WhenSetMetadataThrows()
        {
            // Arrange
            var blobUri = new Uri(_expectedInboxUrl);
            var appInsightsUri = new Uri("https://www.appinsights.com");
            JObject operationContext = JsonHelpers.JsonToJObject("{\"something\": \"something value\"}");
            var metadataData = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestCreateMetadata,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobMetadataCreateDTO { BlobUri = blobUri, BlobMetadata = JObject.FromObject(metadataData), OperationContext = JObject.FromObject(operationContext) })
            };
            var blobBaseClient = new BlobBaseClient(blobUri);
            var blobProperties = new BlobProperties();

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.SetBlobMetadataAsync(blobUri, metadataData, It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(new ArgumentException("Something's wrong"));
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
    }
}