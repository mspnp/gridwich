using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.EventGrid;
using Gridwich.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.Core.EventGridTests
{
    public class EventGridDispatcherTests
    {
        private readonly IEventGridHandler _eventGridHandler;
        private readonly IObjectLogger<EventGridDispatcher> _logger;
        private EventGridDispatcher _eventGridDispatcher;

        public EventGridDispatcherTests()
        {
            _logger = Mock.Of<IObjectLogger<EventGridDispatcher>>();
            _eventGridHandler = Mock.Of<IEventGridHandler>();
            _eventGridDispatcher = new EventGridDispatcher(_logger, new List<IEventGridHandler> { _eventGridHandler });
        }

        [Fact]
        public async Task EventGrid_ShouldNotThrowAndLogException_WhenEventCannotBeHandled()
        {
            // Arrange
            EventGridEvent testEvent = new EventGridEvent();
            List<EventGridEvent> events = new List<EventGridEvent> { testEvent };
            // Arrange Mocks
            Mock.Get(_eventGridHandler)
                .Setup(x => x.HandlesEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException());

            // Act
            var result = await _eventGridDispatcher.DispatchEventGridEvents(events).ConfigureAwait(true);

            // Assert
            result.ShouldBeEquivalentTo(new OkResult());
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.ExceptionHandlingEventGridEvent,
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when general event handling fails");
        }

        [Fact]
        public async Task EventGrid_ShouldNotThrowAndLogException_WhenEventHandlerFails()
        {
            // Arrange
            EventGridEvent testEvent = new EventGridEvent();
            List<EventGridEvent> events = new List<EventGridEvent> { testEvent };
            // Arrange Mocks
            Mock.Get(_eventGridHandler)
                .Setup(x => x.HandlesEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            Mock.Get(_eventGridHandler)
                .Setup(x => x.HandleAsync(It.IsAny<EventGridEvent>()))
                .ThrowsAsync(new InvalidOperationException());

            // Act
            var result = await _eventGridDispatcher.DispatchEventGridEvents(events).ConfigureAwait(true);

            // Assert
            result.ShouldBeEquivalentTo(new OkResult());
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.ExceptionCallingHandlerForEvent,
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the handler fails handling the event");
        }

        [Fact]
        public async Task EventGrid_ShouldIgnoreEvent_WhenNoHandlersCanHandleEvent()
        {
            // Arrange
            EventGridEvent testEvent = new EventGridEvent();
            List<EventGridEvent> events = new List<EventGridEvent> { testEvent };
            // Arrange Mocks
            Mock.Get(_eventGridHandler)
                .Setup(x => x.HandlesEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            // Act
            var result = await _eventGridDispatcher.DispatchEventGridEvents(events).ConfigureAwait(true);

            // Assert
            result.ShouldBeEquivalentTo(new OkResult());
        }

        [Fact]
        public async Task EventGrid_ShouldCallAllValidHandlers_WhenMoreThanOneHandleMatches()
        {
            // Arrange
            EventGridEvent testEvent = new EventGridEvent();
            List<EventGridEvent> events = new List<EventGridEvent> { testEvent };
            var handler1 = Mock.Of<IEventGridHandler>();
            var handler2 = Mock.Of<IEventGridHandler>();
            _eventGridDispatcher = new EventGridDispatcher(_logger, new List<IEventGridHandler> { handler1, handler2 });

            // Arrange Mocks
            Mock.Get(handler1)
                .Setup(x => x.HandlesEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            Mock.Get(handler2)
                .Setup(x => x.HandlesEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _eventGridDispatcher.DispatchEventGridEvents(events).ConfigureAwait(true);

            // Assert
            result.ShouldBeEquivalentTo(new OkResult());
            Mock.Get(handler1).Verify(x =>
                x.HandleAsync(It.IsAny<EventGridEvent>()), Times.Once);
            Mock.Get(handler2).Verify(x =>
                x.HandleAsync(It.IsAny<EventGridEvent>()), Times.Once);
        }

        [Fact]
        public async Task EventGrid_ShouldHandleValidation_WhenValidationEventIsProcessed()
        {
            // Arrange
            string validationCode = "CODE";
            string validationUrl = "URL";
            EventGridEvent testEvent = new EventGridEvent
            {
                EventType = EventTypes.EventGridSubscriptionValidationEvent,
                Data = JObject.FromObject(new SubscriptionValidationEventData(validationCode, validationUrl))
            };
            List<EventGridEvent> events = new List<EventGridEvent> { testEvent };
            // Arrange Mocks
            Mock.Get(_eventGridHandler)
                .Setup(x => x.HandlesEvent(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            // Act
            var result = await _eventGridDispatcher.DispatchEventGridEvents(events).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType<OkObjectResult>();
            var validationResponse = (OkObjectResult)result;
            validationResponse.Value.ShouldBeOfType<SubscriptionValidationResponse>();
            var responseValue = (SubscriptionValidationResponse)validationResponse.Value;
            responseValue.ValidationResponse.ShouldBe(validationCode);
        }
    }
}