using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class ChangeBlobTierHandlerTests
    {
        private readonly ChangeBlobTierHandler _handler;
        private readonly IObjectLogger<ChangeBlobTierHandler> _logger;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;

        public ChangeBlobTierHandlerTests()
        {
            JsonHelpers.SetupJsonSerialization();
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _logger = Mock.Of<IObjectLogger<ChangeBlobTierHandler>>();
            _handler = new ChangeBlobTierHandler(
                _logger,
                _eventGridPublisher,
                _storageService);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "673572f4-1e57-4b32-a7e6-57ae433eb9c1";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(CustomEventTypes.RequestBlobTierChange, "1.0")]
        [InlineData(CustomEventTypes.RequestBlobTierChange, "2.0")]
        [InlineData(CustomEventTypes.RequestBlobTierChange, "NotAnInt")]
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
        [InlineData(CustomEventTypes.ResponseBlobCreatedSuccess, "1.0")]
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
            var blobUri = "https://gridwichasset00sasb.com/container1/la_macarena.mp4";
            var changeBlobTierData = new RequestBlobTierChangeDTO { AccessTier = BlobAccessTier.Archive, BlobUri = blobUri, RehydratePriority = BlobRehydratePriority.High };
            var context = TestHelpers.CreateGUIDContext();
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobTierChange,
                DataVersion = "1.0",
                Data = JObject.FromObject(changeBlobTierData)
            };
            EventGridEvent resultEvent = null;

            var x = LogEventIds.AboutToAttemptPublishOfEventWithId;

            // Arrange Mocks
            Mock.Get(_storageService)
                .Setup(x => x.ChangeBlobTierAsync(new Uri(blobUri), changeBlobTierData.AccessTier, changeBlobTierData.RehydratePriority, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => resultEvent = eventGridEvent)
                .ReturnsAsync(true);
            Mock.Get(_logger)
                .Setup(x => x.LogExceptionObject(
                    out appInsightsUri,
                    It.IsAny<EventId>(),
                    It.IsAny<Exception>(),
                    It.IsAny<object>()));

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(It.IsAny<EventId>(), It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Never);
            handleAsyncResult.ShouldBe(true);
            resultEvent.ShouldNotBeNull();
            resultEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobTierChanged);
            var resultEventData = (ResponseBlobTierChangeSuccessDTO)resultEvent.Data;
            resultEventData.AccessTier.ShouldBe(changeBlobTierData.AccessTier);
            resultEventData.BlobUri.ShouldBe(changeBlobTierData.BlobUri);
            resultEventData.RehydratePriority.ShouldBe(changeBlobTierData.RehydratePriority);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnFalse_WhenPublishingThrows()
        {
            // Arrange
            var blobUri = "https://gridwichasset00sasb.com/container1/la_macarena.mp4";
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var changeBlobTierData = new RequestBlobTierChangeDTO { AccessTier = BlobAccessTier.Archive, BlobUri = blobUri, RehydratePriority = BlobRehydratePriority.High };
            var context = TestHelpers.CreateGUIDContext();
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobTierChange,
                DataVersion = "1.0",
                Data = JObject.FromObject(changeBlobTierData)
            };

            // Arrange Mocks
            Mock.Get(_storageService)
                .Setup(x => x.ChangeBlobTierAsync(new Uri(blobUri), changeBlobTierData.AccessTier, changeBlobTierData.RehydratePriority, context))
                .ReturnsAsync(true);
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ThrowsAsync(new InvalidOperationException());
            Mock.Get(_logger)
                .Setup(x => x.LogExceptionObject(
                    out appInsightsUri,
                    LogEventIds.FailedCriticallyToPublishEvent,
                    It.IsAny<Exception>(),
                    It.IsAny<object>()));
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
                    x.LogExceptionObject(LogEventIds.FailedCriticallyToPublishEvent, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_ShouldPublishFailureEvent_WhenChangeTierThrows()
        {
            // Arrange
            var blobUri = "https://gridwichasset00sasb.com/container1/la_macarena.mp4";
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var context = TestHelpers.CreateGUIDContext();
            var changeBlobTierData = new RequestBlobTierChangeDTO
            {
                AccessTier = BlobAccessTier.Archive,
                BlobUri = blobUri,
                RehydratePriority = BlobRehydratePriority.High,
                OperationContext = context.ClientRequestIdAsJObject
            };
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobTierChange,
                EventTime = DateTime.UtcNow,
                DataVersion = "1.0",
                Data = JObject.FromObject(changeBlobTierData)
            };

            // Arrange Mocks
            var expectedEventToPublishCaptureMatch = new CaptureMatch<EventGridEvent>(x =>
            {
                // Assert values in the object passed to the publisher:
                x.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
                x.Data.ShouldBeOfType(typeof(ResponseFailureDTO));
                var data = (ResponseFailureDTO)x.Data;
                data.ShouldNotBeNull();
            });
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(Capture.With(expectedEventToPublishCaptureMatch)))
                .ReturnsAsync(true);

            Mock.Get(_storageService)
                .Setup(x => x.ChangeBlobTierAsync(new Uri(blobUri), changeBlobTierData.AccessTier, changeBlobTierData.RehydratePriority, It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(new InvalidOperationException());
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
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldPublishFailureEvent_WhenAccessTierIsInvalid()
        {
            // Arrange
            var accessTier = BlobAccessTier.Lookup("Invalid");
            var blobUri = "https://gridwichasset00sasb.com/container1/la_macarena.mp4";
            var appInsightsUri = new Uri("https://www.appinsights.com");
            var changeBlobTierData = new RequestBlobTierChangeDTO { AccessTier = accessTier, BlobUri = blobUri, RehydratePriority = BlobRehydratePriority.High };
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestBlobTierChange,
                EventTime = DateTime.UtcNow,
                DataVersion = "1.0",
                Data = JObject.FromObject(changeBlobTierData)
            };

            var expectedEventToPublishCaptureMatch = new CaptureMatch<EventGridEvent>(x =>
            {
                // Assert values in the object passed to the publisher:
                x.EventTime.ShouldBeInRange(testEvent.EventTime, testEvent.EventTime.AddMinutes(1));
                x.Data.ShouldBeOfType(typeof(ResponseFailureDTO));
                var data = (ResponseFailureDTO)x.Data;
                data.ShouldNotBeNull();
            });

            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(Capture.With(expectedEventToPublishCaptureMatch)))
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
        }

        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenInvalidEvent()
        {
            // Arrange
            var accessTier = BlobAccessTier.Lookup("Invalid");
            var blobUri = "https://gridwichasset00sasb.com/container1/la_macarena.mp4";
            var changeBlobTierData = new RequestBlobTierChangeDTO { AccessTier = accessTier, BlobUri = blobUri, RehydratePriority = BlobRehydratePriority.High };
            var testEvent = new EventGridEvent
            {
                EventType = CustomEventTypes.RequestCreateMetadata,
                DataVersion = "1.0",
                Data = JObject.FromObject(changeBlobTierData)
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _handler.HandleAsync(testEvent).ConfigureAwait(true)).ConfigureAwait(true);
        }
    }
}