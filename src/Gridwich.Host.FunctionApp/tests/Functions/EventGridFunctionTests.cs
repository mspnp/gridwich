using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Host.FunctionApp.Functions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Moq;

using Newtonsoft.Json.Linq;

using Shouldly;

using Xunit;

namespace Gridwich.Host.FunctionAppTests.Functions
{
    public sealed class EventGridFunctionTests : IDisposable
    {
        private readonly IEventGridDispatcher _eventDispatcher;
        private readonly EventGridFunction _eventGridFunction;
        private readonly IObjectLogger<EventGridFunction> _logger;
        private MemoryStream _eventMemoryStream;

        public EventGridFunctionTests()
        {
            _logger = Mock.Of<IObjectLogger<EventGridFunction>>();
            _eventDispatcher = Mock.Of<IEventGridDispatcher>();
            _eventGridFunction = new EventGridFunction(_logger, _eventDispatcher);
        }

        public void Dispose()
        {
            _eventMemoryStream?.Dispose();
        }

        [Fact]
        public async Task EventGridFunction_ShouldCallDispatcher_WhenEndpointIsCalled()
        {
            // Arrange
            var events = new List<EventGridEvent>
            {
                new EventGridEvent()
            };
            var httpRequest = new Mock<HttpRequest>();
            var expected = new OkResult();

            // Arrange Mocks
            SetupGetEventsAsBodyOfHttpRequest(events, httpRequest);
            Mock.Get(_eventDispatcher)
                .Setup(x => x.DispatchEventGridEvents(It.IsAny<List<EventGridEvent>>()))
                .ReturnsAsync(expected);

            // Act
            var result = await _eventGridFunction.Run(httpRequest.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBe(expected);

            // Cleanup Test
            _eventMemoryStream.Dispose();
            _eventMemoryStream = null;
        }

        [Fact]
        public async Task EventGridFunction_ShouldReturnBadRequest_WhenHttpRequestIsNull()
        {
            // Arrange
            HttpRequest httpRequest = null;
            var expectedType = typeof(BadRequestObjectResult);

            // Act
            var result = await _eventGridFunction.Run(httpRequest, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType(expectedType);
            Mock.Get(_logger)
                .Verify(x =>
                    x.LogEvent(LogEventIds.EventGridFunctionGotNullHttpRequest,
                        It.IsAny<string>()),
                    Times.Once,
                    "An error should be logged.");
        }

        [Fact]
        public async Task EventGridFunction_ShouldReturnBadRequest_WhenHttpRequestBodyIsNull()
        {
            // Arrange
            var httpRequest = new Mock<HttpRequest>();
            var expectedType = typeof(BadRequestObjectResult);

            // Arrange Mocks
            MemoryStream ms = null;
            httpRequest.SetupGet(x => x.Body).Returns(ms);

            // Act
            var result = await _eventGridFunction.Run(httpRequest.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType(expectedType);
            Mock.Get(_logger)
                .Verify(x =>
                    x.LogEvent(LogEventIds.EventGridFunctionGotNullHttpRequestBody,
                        It.IsAny<string>()),
                    Times.Once,
                    "An error should be logged.");
        }

        [Fact]
        public async Task EventGridFunction_ShouldReturnBadRequest_WhenHttpRequestBodyIsAlreadyDisposed()
        {
            // Arrange
            var httpRequest = new Mock<HttpRequest>();
            var expectedType = typeof(BadRequestObjectResult);

            // Arrange Mocks
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("[]"));
            httpRequest.SetupGet(x => x.Body).Returns(ms);
            ms.Dispose();

            // Act
            var result = await _eventGridFunction.Run(httpRequest.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType(expectedType);
            Mock.Get(_logger)
                .Verify(x =>
                    x.LogEventObject(LogEventIds.EventGridFunctionExceptionReadingHttpRequestBody,
                        It.IsAny<ArgumentException>()),
                    Times.Once,
                    "An error should be logged.");
        }

        [Fact]
        public async Task EventGridFunction_ShouldReturnBadRequest_WhenHttpRequestBodyIsEmpty()
        {
            // Arrange
            var httpRequest = new Mock<HttpRequest>();
            var expectedType = typeof(BadRequestObjectResult);

            // Arrange Mocks
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            httpRequest.SetupGet(x => x.Body).Returns(ms);

            // Act
            var result = await _eventGridFunction.Run(httpRequest.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType(expectedType);
            Mock.Get(_logger)
                .Verify(x =>
                    x.LogEvent(LogEventIds.EventGridFunctionGotEmptyBody,
                        It.IsAny<string>()),
                    Times.Once,
                    "An error should be logged.");

            // Cleanup Test
            ms.Dispose();
            ms = null;
        }

        [Fact]
        public async Task EventGridFunction_ShouldReturnBadRequest_WhenBodyIsNotParsable()
        {
            // Arrange
            var httpRequest = new Mock<HttpRequest>();
            var expectedType = typeof(BadRequestObjectResult);

            // Arrange Mocks
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("This is not JSON}}}"));
            httpRequest.SetupGet(x => x.Body).Returns(ms);

            // Act
            var result = await _eventGridFunction.Run(httpRequest.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType(expectedType);
            Mock.Get(_logger)
                .Verify(x =>
                    x.LogEventObject(LogEventIds.EventGridFunctionGotUnparsableBody,
                        It.IsAny<object>()),
                    Times.Once,
                    "An error should be logged.");

            // Cleanup Test
            ms.Dispose();
            ms = null;
        }

        [Fact]
        public async Task EventGridFunction_ShouldReturnBadRequest_WhenHttpRequestBodyIsEmptyJsonArray()
        {
            // Arrange
            var httpRequest = new Mock<HttpRequest>();
            var expectedType = typeof(BadRequestObjectResult);

            // Arrange Mocks
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("[]"));
            httpRequest.SetupGet(x => x.Body).Returns(ms);

            // Act
            var result = await _eventGridFunction.Run(httpRequest.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            result.ShouldBeOfType(expectedType);
            Mock.Get(_logger)
                .Verify(x =>
                    x.LogEvent(LogEventIds.EventGridFunctionGotEmptyArrayAsBody,
                        It.IsAny<string>()),
                    Times.Once,
                    "An error should be logged.");

            // Cleanup Test
            ms.Dispose();
            ms = null;
        }

        /// <summary>
        /// Use this to convert events into a memory stream to be read when httpRequest.Body is called.
        /// </summary>
        /// <param name="events">The list of events to be return when .Body is called.</param>
        /// <param name="httpRequest">The mocked httpRequest object.</param>
        private void SetupGetEventsAsBodyOfHttpRequest(List<EventGridEvent> events, Mock<HttpRequest> httpRequest)
        {
            var content = JArray.FromObject(events).ToString();
            _eventMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            httpRequest.SetupGet(x => x.Body).Returns(_eventMemoryStream);
        }
    }
}