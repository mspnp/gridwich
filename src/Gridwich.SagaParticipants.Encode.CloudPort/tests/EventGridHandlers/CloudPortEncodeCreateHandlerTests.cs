using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.CloudPort.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.CloudPort.Services;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.CloudPortTests.EventGridHandlers
{
    public class CloudPortEncodeCreateHandlerTests
    {
        private readonly IObjectLogger<CloudPortEncodeCreateHandler> logger = Mock.Of<IObjectLogger<CloudPortEncodeCreateHandler>>();
        private readonly IEventGridPublisher eventGridPublisher = Mock.Of<IEventGridPublisher>();
        private readonly CloudPortEncodeCreateHandler handler;
        private readonly ICloudPortService cloudPortService = Mock.Of<ICloudPortService>();

        public CloudPortEncodeCreateHandlerTests()
        {
            handler = new CloudPortEncodeCreateHandler(logger, eventGridPublisher, cloudPortService);
        }

        /// <summary>
        /// Initialize expectedHandlerId and actualHandlerId.
        /// </summary>
        [Fact]
        public void GetHandlerIdShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "4F33F10F-13CC-457C-AD9C-BA8A5CB5518D";

            // Act
            var actualHandlerId = this.handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }


        /// <summary>
        /// Successfull Event handling <see cref="CloudPortEncodeCreateHandler"/> class.
        /// </summary>
        /// <param name="eventType">EventType or the test.</param>
        /// <param name="dataVersion">Data Version for the test.</param>
        /// <param name="shouldbe">Expected return value.</param>
        [Theory]
        [InlineData(CustomEventTypes.RequestEncodeCloudPortCreate, "1.0", true)]
        public void HandlesEventShouldHandleEventTypeandVersion(string eventType, string dataVersion, bool shouldbe)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = this.handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBe(shouldbe);
        }

        /// <summary>
        /// Successfull Event handling  <see cref="CloudPortEncodeCreateHandler"/> class.
        /// </summary>
        /// <param name="eventType">EventType or the test.</param>
        /// <param name="dataVersion">Data Version for the test.</param>
        [Theory]
        [InlineData(CustomEventTypes.ResponseBlobAnalysisSuccess, "1.0")]
        [InlineData("NotAnExpectedEventType", "1.0")]
        [InlineData("NotAnExpectedEventType", "NotAnInt")]
        public void HandlesEventShouldNotHandleEventTypeandVersion(string eventType, string dataVersion)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = this.handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBeFalse();
        }
    }
}
