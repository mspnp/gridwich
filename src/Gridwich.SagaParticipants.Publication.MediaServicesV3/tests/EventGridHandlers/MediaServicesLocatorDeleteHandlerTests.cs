using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.EventGridHandlers;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3Tests.EventGridHandlers
{
    /// <summary>
    /// Test class MediaServicesLocatorDeleteHandlerTests <see cref="MediaServicesLocatorDeleteHandler"/> class.
    /// </summary>
    ///
    [ExcludeFromCodeCoverage]
    public class MediaServicesLocatorDeleteHandlerTests
    {
        private readonly IObjectLogger<MediaServicesLocatorDeleteHandler> logger;
        private readonly IEventGridPublisher eventGridPublisher;
        private readonly MediaServicesLocatorDeleteHandler handler;
        private readonly IMediaServicesV3PublicationService mediaServicesV3PublicationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesLocatorDeleteHandlerTests"/> class.
        /// </summary>
        public MediaServicesLocatorDeleteHandlerTests()
        {
            logger = Mock.Of<IObjectLogger<MediaServicesLocatorDeleteHandler>>();
            eventGridPublisher = Mock.Of<IEventGridPublisher>();
            mediaServicesV3PublicationService = Mock.Of<IMediaServicesV3PublicationService>();
            handler = new MediaServicesLocatorDeleteHandler(logger, eventGridPublisher, mediaServicesV3PublicationService);
        }

        /// <summary>
        /// Initialize expectedHandlerId and actualHandlerId.
        /// </summary>
        [Fact]
        public void GetHandlerIdShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "ff552563-5c1a-445a-ac7d-32fe6b89f865";

            // Act
            var actualHandlerId = handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        /// <summary>
        /// Successfull Event handling <see cref="MediaServicesLocatorDeleteHandler"/> class.
        /// </summary>
        /// <param name="eventType">EventType or the test.</param>
        /// <param name="dataVersion">Data Version for the test.</param>
        /// <param name="shouldbe">Expected return value.</param>
        [Theory]
        [InlineData(CustomEventTypes.RequestMediaservicesLocatorDelete, "1.0", true)]
        public void HandlesEventShouldHandleEventTypeandVersion(string eventType, string dataVersion, bool shouldbe)
        {
            // Arrange
            // See InlineData

            // Act
            bool eventHandled = handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBe(shouldbe);
        }

        /// <summary>
        /// Successfull Event handling  <see cref="MediaServicesV3EncoderStatusHandler"/> class.
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
            bool eventHandled = handler.HandlesEvent(eventType, dataVersion);

            // Assert:
            eventHandled.ShouldBeFalse();
        }
    }
}
