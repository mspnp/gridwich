using System;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;

using Moq;

using Newtonsoft.Json;

using Shouldly;

using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class BlobSasUrlCreateHandlerTests
    {
        private readonly BlobSasUrlCreateHandler _handler;
        private readonly IObjectLogger<BlobSasUrlCreateHandler> _logger;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;

        public BlobSasUrlCreateHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobSasUrlCreateHandler>>();

            _handler = new BlobSasUrlCreateHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "dfe336ce-4d21-11ea-bb6e-cfd5a68b8278";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(CustomEventTypes.RequestBlobSasUrlCreate, "1.0")]
        [InlineData(CustomEventTypes.RequestBlobSasUrlCreate, "2.0")]
        [InlineData(CustomEventTypes.RequestBlobSasUrlCreate, "NotAnInt")]
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
        [InlineData(CustomEventTypes.ResponseAcknowledge, "1.0")]
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
            const string BLOB_URL = "https://some.com/path/to/a/blob.mp4";
            const string BLOB_SAS_URL = "https://sas-url-for-your-blob.com";
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobSasUrlCreate,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new RequestBlobSasUrlCreateDTO { BlobUri = new Uri(BLOB_URL), SecToLive = 10 })
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetSasUrlForBlob(It.IsAny<Uri>(), It.IsAny<TimeSpan>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(BLOB_SAS_URL);
            var expectedEventToPublishCaptureMatch = new CaptureMatch<EventGridEvent>(x =>
            {
                // Assert values in the object passed to the publisher:
                x.EventType.ShouldBe(CustomEventTypes.ResponseBlobSasUrlSuccess);
                x.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
                x.Data.ShouldBeOfType(typeof(ResponseBlobSasUrlSuccessDTO));
                var data = (ResponseBlobSasUrlSuccessDTO)x.Data;
                // TODO: make test pass with this assertion.
                // data.SasUrl.ToString().ShouldBe(BLOB_SAS_URL);
            });
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(Capture.With(expectedEventToPublishCaptureMatch)))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
        }

        [Fact]
        public async Task HandleAsync_ShouldErrorOnBadInput()
        {
            // Arrange
            const string blobSasUrl = null;
            var topicEndpointUri = new Uri("https://www.topichost.com");
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var testEvent = new EventGridEvent
            {
                EventTime = DateTime.UtcNow,
                EventType = CustomEventTypes.RequestBlobSasUrlCreate,
                DataVersion = "1.0",
                Data = JsonConvert.SerializeObject(new { foo = "bar" })
            };

            // Arrange Mocks
            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(Publishing.TopicOutboundEndpointSettingName))
                .Returns(topicEndpointUri.ToString());
            Mock.Get(_storageService)
                .Setup(x => x.GetSasUrlForBlob(It.IsAny<Uri>(), It.IsAny<TimeSpan>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(blobSasUrl);
            Mock.Get(_logger)
                .Setup(x => x.LogExceptionObject(
                    out appInsightsUri,
                    LogEventIds.GridwichUnhandledException,
                    It.IsAny<Exception>(),
                    It.IsAny<object>()));
            var expectedEventToPublishCaptureMatch = new CaptureMatch<EventGridEvent>(x =>
            {
                // Assert values in the object passed to the publisher:
                x.EventType.ShouldBe(CustomEventTypes.ResponseBlobSasUrlSuccess);
                x.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
                x.Data.ShouldBeOfType(typeof(ResponseBlobSasUrlSuccessDTO));
                var data = (ResponseBlobSasUrlSuccessDTO)x.Data;
                // TODO: make test pass with this assertion.
                // data.SasUrl.ToString().ShouldBe(BLOB_SAS_URL);
            });
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(Capture.With(expectedEventToPublishCaptureMatch)))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false, "handleAsync should return false");
        }
    }
}
