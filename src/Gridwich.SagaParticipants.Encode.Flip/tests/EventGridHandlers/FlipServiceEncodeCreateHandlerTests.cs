using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.Flip.EventGridHandlers;
using Gridwich.SagaParticipants.Encode.Flip.Services;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.FlipTests.EventGridHandlers
{
    public class FlipServiceEncodeCreateHandlerTests
    {
        private readonly IObjectLogger<FlipServiceEncodeCreateHandler> logger = Mock.Of<IObjectLogger<FlipServiceEncodeCreateHandler>>();
        private readonly IEventGridPublisher eventGridPublisher = Mock.Of<IEventGridPublisher>();
        private readonly FlipServiceEncodeCreateHandler handler;
        private readonly IFlipService cloudPortService = Mock.Of<IFlipService>();

        public FlipServiceEncodeCreateHandlerTests()
        {
            handler = new FlipServiceEncodeCreateHandler(logger, eventGridPublisher, cloudPortService);
        }

        /// <summary>
        /// Initialize expectedHandlerId and actualHandlerId.
        /// </summary>
        [Fact]
        public void GetHandlerIdShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "772A7381-5EDE-4602-B788-BBA89B211A93";

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
        [InlineData(CustomEventTypes.RequestEncodeFlipCreate, "1.0", true)]
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
