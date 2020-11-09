using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.EventGridHandlers;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3Tests.EventGridHandlers
{
    /// <summary>
    /// Test class MediaServicesV3EncoderStatusHandlerTests <see cref="MediaServicesLocatorCreateHandler"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MediaServicesLocatorCreateHandlerTests
    {
        private readonly IObjectLogger<MediaServicesLocatorCreateHandler> logger;
        private readonly IEventGridPublisher eventGridPublisher;
        private readonly MediaServicesLocatorCreateHandler handler;
        private readonly IMediaServicesV3PublicationService mediaServicesV3PublicationService;

        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsData
        {
            get
            {
                return new[]
                {
                    new object[] { MediaServicesV3PublicationTestData.RequestMediaServicesLocatorCreateDTO_Is_Expected, CustomEventTypes.ResponseMediaservicesLocatorCreateSuccess, null }
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesLocatorCreateHandlerTests"/> class.
        /// </summary>
        public MediaServicesLocatorCreateHandlerTests()
        {
            logger = Mock.Of<IObjectLogger<MediaServicesLocatorCreateHandler>>();
            eventGridPublisher = Mock.Of<IEventGridPublisher>();
            mediaServicesV3PublicationService = Mock.Of<IMediaServicesV3PublicationService>();
            handler = new MediaServicesLocatorCreateHandler(logger, eventGridPublisher, mediaServicesV3PublicationService);
        }

        /// <summary>
        /// Initialize expectedHandlerId and actualHandlerId.
        /// </summary>
        [Fact]
        public void GetHandlerIdShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "1CC572f4-1e57-4b32-a7e6-57ae433eb9c1";

            // Act
            var actualHandlerId = handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        /// <summary>
        /// Successfull Event handling <see cref="MediaServicesLocatorCreateHandler"/> class.
        /// </summary>
        /// <param name="eventType">EventType or the test.</param>
        /// <param name="dataVersion">Data Version for the test.</param>
        /// <param name="shouldbe">Expected return value.</param>
        [Theory]
        [InlineData(CustomEventTypes.RequestMediaservicesLocatorCreate, "1.0", true)]
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

        /// <summary>
        /// Testing the Media Services V3 Create Locator Handler.
        /// </summary>
        /// <param name="baseDTO">RequestBaseDTO data.</param>
        /// <param name="expectedCustomEventTypes">Expected <see cref="CustomEventTypes"/>.</param>
        /// <param name="expectedExceptionType">Expected type of Exception.</param>
        [Theory]
        [MemberData(nameof(OperationsData))]
        public async void MediaServicesLocatorCreateHandler_DoWorkAsync_Tests(RequestMediaServicesLocatorCreateDTO baseDTO, string expectedCustomEventTypes, Type expectedExceptionType)
        {
            // Arrange Mocks
            Mock.Get(eventGridPublisher)
               .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
               .ReturnsAsync(true);

            Mock.Get(mediaServicesV3PublicationService)
                .Setup(x => x.LocatorCreateAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeBasedFilterDTO>(),
                    It.IsAny<JObject>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(MediaServicesV3PublicationTestData.ServiceOperationResult_Is_Expected);

            // Act
            ResponseBaseDTO eventReturned = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                eventReturned = await this.handler.TestDoWorkAsync(baseDTO, CustomEventTypes.RequestMediaservicesLocatorCreate).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            if (expectedExceptionType == null)
            {
                // Success cases
                exception.ShouldBeNull();
            }
            else
            {
                // Failure cases
                exception.ShouldBeOfType(expectedExceptionType);
                eventReturned.ShouldBeNull();
            }

            if (!string.IsNullOrWhiteSpace(expectedCustomEventTypes))
            {
                // Success cases
                eventReturned.ShouldNotBeNull();
                eventReturned.ReturnEventType.ShouldBe(expectedCustomEventTypes);
            }
            else
            {
                // Failure cases
                eventReturned.ShouldBeNull();
            }
        }

        /// <summary>
        /// Exceptions thrown by lower-level services should pass through.
        /// </summary>
        [Fact]
        public async void MediaServicesLocatorCreateHandler_Exceptions_Passthrough_Test()
        {
            // Arrange
            var expectedExceptionType = typeof(GridwichPublicationLocatorCreationException);

            // Arrange Mocks
            Mock.Get(eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            Mock.Get(mediaServicesV3PublicationService)
                .Setup(x => x.LocatorCreateAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeBasedFilterDTO>(),
                    It.IsAny<JObject>(),
                    It.IsAny<bool>()))
                .ThrowsAsync(new GridwichPublicationLocatorCreationException(
                    MediaServicesV3PublicationTestData.GoodAssetName,
                    "Something went wrong.",
                    innerException: new Exception()));

            // Act
            ResponseBaseDTO eventReturned = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                eventReturned = await this.handler.TestDoWorkAsync(MediaServicesV3PublicationTestData.RequestMediaServicesLocatorCreateDTO_Is_Expected, CustomEventTypes.RequestMediaservicesLocatorCreate).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldBeOfType(expectedExceptionType);
            eventReturned.ShouldBeNull();
        }
    }
}
