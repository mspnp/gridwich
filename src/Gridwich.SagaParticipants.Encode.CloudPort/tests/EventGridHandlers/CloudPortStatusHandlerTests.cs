using System;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.CloudPort.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.CloudPort.Models;
using Gridwich.SagaParticipants.Encode.CloudPort.Services;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.CloudPortTests.EventGridHandlers
{
    public class CloudPortStatusHandlerTests
    {
        private readonly IObjectLogger<CloudPortStatusHandler> _logger = Mock.Of<IObjectLogger<CloudPortStatusHandler>>();
        private readonly IEventGridPublisher _eventGridPublisher = Mock.Of<IEventGridPublisher>();
        private readonly CloudPortStatusHandler _handler;

        private static readonly IStorageService _storageService = Mock.Of<IStorageService>();
        private static readonly ICloudPortService _encoder1 = Mock.Of<ICloudPortService>();

        public CloudPortStatusHandlerTests()
        {
            _handler = new CloudPortStatusHandler(_logger, _eventGridPublisher, _encoder1, _storageService);
        }

        [Fact]
        public void GetHandlerIdShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "7E995F77-5665-4C72-ACAD-FAC9E166D5E9";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobCreated, "1.0", true)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobCreated, "2.0", false)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobError, "1.0", true)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobError, "2.0", false)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobProgress, "1.0", true)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobProgress, "2.0", false)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobSuccess, "1.0", true)]
        [InlineData(ExternalEventTypes.CloudPortWorkflowJobSuccess, "2.0", false)]
        public void HandlesEventShouldHandleEventTypeAndVersion(string eventType, string dataVersion, bool shouldbe)
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
        public void HandlesEventShouldNotHandleEventTypeAndVersion(string eventType, string dataVersion)
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
        public async Task HandleAsyncShouldReturnTrueAndNotLogWhenNoErrors()
        {
            // Arrange
            var payload = new CloudPortPayload()
            {
                OperationContext = testOpCtx,
                OutputContainer = "https://someaccount.blob.core.windows.net/somecontainer"
            };
            var cloudPortStatusData = new CloudPortStatusData()
            {
                Name = "Dave",
                Id = "242",
                Progress = 42,
                State = "Completed",
                Status = "Succeded",
                WorkflowId = "222",
                Payload = payload,
                ActionJobs = new object[] { new object(), new object() }
            };

            var eventToPublish = new EventGridEvent()
            {
                Id = Guid.NewGuid().ToString(),
                Data = JObject.FromObject(cloudPortStatusData),
                EventTime = DateTime.Now,
                EventType = ExternalEventTypes.CloudPortWorkflowJobProgress,
                Subject = $"/EncodeCompleted/sagaid",
                DataVersion = "1.0",
            };

            // Arrange Mocks
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(eventToPublish).ConfigureAwait(false);

            // Assert
            handleAsyncResult.ShouldBe(true, "handleAsync should always return true");
        }
    }
}
