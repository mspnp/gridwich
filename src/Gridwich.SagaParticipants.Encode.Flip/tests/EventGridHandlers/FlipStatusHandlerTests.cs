using System;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Flip.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.Flip.Models;
using Gridwich.SagaParticipants.Encode.Flip.Services;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.FlipTests.EventGridHandlers
{
    public class FlipStatusHandlerTests
    {
        private readonly IObjectLogger<FlipStatusHandler> _logger;
        private static readonly IEventGridPublisher _eventGridPublisher = Mock.Of<IEventGridPublisher>();

        private static readonly IStorageService _storageService = Mock.Of<IStorageService>();

        private static readonly IFlipService _flipService = Mock.Of<IFlipService>();

        private readonly FlipStatusHandler _handler;

        public FlipStatusHandlerTests()
        {
            _logger = Mock.Of<IObjectLogger<FlipStatusHandler>>();
            _handler = new FlipStatusHandler(_logger, _eventGridPublisher, _flipService, _storageService);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "99995F77-5665-4C72-ACAD-FAC9DEADBEEF";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(ExternalEventTypes.FlipVideoCreated, "1.0", true)]
        [InlineData(ExternalEventTypes.FlipVideoCreated, "2.0", true)]
        [InlineData(ExternalEventTypes.FlipVideoEncoded, "1.0", false)]
        [InlineData(ExternalEventTypes.FlipVideoEncoded, "2.0", false)]
        [InlineData(ExternalEventTypes.FlipEncodingProgress, "1.0", true)]
        [InlineData(ExternalEventTypes.FlipEncodingProgress, "2.0", true)]
        [InlineData(ExternalEventTypes.FlipEncodingComplete, "1.0", true)]
        [InlineData(ExternalEventTypes.FlipEncodingComplete, "2.0", true)]
        public void HandlesEvent_ShouldHandle_EventType_and_Version(string eventType, string dataVersion, bool shouldbe)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = _handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBe(shouldbe);
        }

        [Theory]
        [InlineData(CustomEventTypes.ResponseBlobAnalysisSuccess, "1.0")]
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

        // A simple context..
        private readonly JObject testOpCtx = JsonHelpers.DeserializeOperationContext("{ \"Made up in FlipEncodeCompleteHandlerTests.cs\":666 }");

        [Fact]
        public async Task HandleAsync_ShouldReturnTrueAndNotLog_WhenNoErrorsFailedEncode()
        {
            // Arrange
            var payload = new FlipPayload()
            {
                OperationContext = testOpCtx,
                FactoryId = "424242",
                OutputContainer = "https://someaccount.blob.core.windows.net/somecontainer"
            };
            var flipEncodeCompleteData = new FlipEncodingCompleteData()
            {
                EncodingId = "444",
                EncodingStatus = "777",
                OriginalFilename = "bbb.mp4",
                EventName = "flip",
                ServiceName = "flip",
                VideoId = "999",
                VideoPayload = payload,
            };
            var appInsightsUri = new Uri("https://www.appinsights.com");

            var eventToPublish = new EventGridEvent()
            {
                Id = Guid.NewGuid().ToString(),
                Data = JObject.FromObject(flipEncodeCompleteData),
                EventTime = DateTime.Now,
                EventType = ExternalEventTypes.FlipEncodingComplete,
                Subject = $"/EncodeCompleted/sagaid",
                DataVersion = "1.0",
            };

            var expectedEventToPublishCaptureMatch = new CaptureMatch<EventGridEvent>(x =>
            {
                // Assert values in the object passed to the publisher:
                x.EventType.ShouldBe(CustomEventTypes.ResponseFailure);
            });

            // Arrange Mocks
            Mock.Get(_eventGridPublisher).Setup(x => x.PublishEventToTopic(Capture.With(expectedEventToPublishCaptureMatch)))
                .ReturnsAsync(true);
            Mock.Get(_logger)
                .Setup(x => x.LogEventObject(
                    out appInsightsUri,
                    LogEventIds.EncodeCompleteFailure,
                    It.IsAny<object>()));
            Mock.Get(_flipService)
                .Setup(x => x.GetEncodeInfo(It.IsAny<FlipEncodingCompleteData>()))
                .Returns(new Telestream.Cloud.Flip.Model.Encoding { ErrorClass = "ErrorClass", ErrorMessage = "ErrorMessage" });

            // Act
            var handleAsyncResult = await _handler.HandleAsync(eventToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return false");
        }
    }
}
