using System;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class ContainerCreateHandlerTests
    {
        private const string _storageAccountName = "gridwichinbox00saprm1";
        private readonly ContainerCreateHandler _handler;
        private readonly IObjectLogger<ContainerCreateHandler> _logger;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;

        public ContainerCreateHandlerTests()
        {
            JsonHelpers.SetupJsonSerialization();
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _logger = Mock.Of<IObjectLogger<ContainerCreateHandler>>();
            _handler = new ContainerCreateHandler(
                _logger,
                _eventGridPublisher,
                _storageService);
        }

        [Fact]
        public void GetHandlerId_ShouldBeExpectedValueAndType()
        {
            // Arrange
            string expectedHandlerId = "24b0230e-b00f-7fae-e505-8ea18fd0b4e8";

            // Act
            var actualHandlerId = _handler.GetHandlerId();

            // Assert:
            actualHandlerId.ShouldBeOfType(typeof(string));
            actualHandlerId.ShouldBe(expectedHandlerId);
        }

        [Theory]
        [InlineData(CustomEventTypes.RequestBlobContainerCreate, "1.0")]
        [InlineData(CustomEventTypes.RequestBlobContainerCreate, "2.0")]
        [InlineData(CustomEventTypes.RequestBlobContainerCreate, "NotAnInt")]
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
            var createContainerData = new RequestContainerCreateDTO { StorageAccountName = _storageAccountName, ContainerName = "unittestcontainer" + Guid.NewGuid().ToString() };
            var blobContainerCreateId = Guid.NewGuid().ToString();
            var testEvent =
                new EventGridEvent
                {
                    Data = JObject.FromObject(createContainerData),
                    EventType = CustomEventTypes.RequestBlobContainerCreate,
                    DataVersion = "1.0",
                };
            // Arrange Mocks
            Mock.Get(_storageService)
                .Setup(x => x.ContainerCreateAsync(createContainerData.StorageAccountName, createContainerData.ContainerName, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);

            EventGridEvent resultEvent = null;
            Mock.Get(_eventGridPublisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .Callback<EventGridEvent>((eventGridEvent) => resultEvent = eventGridEvent)
                .ReturnsAsync(true);

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(true);
            resultEvent.ShouldNotBeNull();
            resultEvent.EventType.ShouldBe(CustomEventTypes.ResponseBlobContainerSuccess);
            var resultEventData = (ResponseContainerCreatedSuccessDTO)resultEvent.Data;
            resultEventData.StorageAccountName.ShouldBe(createContainerData.StorageAccountName);
            resultEventData.ContainerName.ShouldBe(createContainerData.ContainerName);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnFalse_WhenContainerCreateThrows()
        {
            // Arrange
            var createContainerData = new RequestContainerCreateDTO { StorageAccountName = _storageAccountName, ContainerName = "unittestcontainer" + Guid.NewGuid().ToString() };
            var blobContainerCreateId = Guid.NewGuid().ToString();
            var testEvent =
                new EventGridEvent
                {
                    Data = JObject.FromObject(createContainerData),
                    EventType = CustomEventTypes.RequestBlobContainerCreate,
                    DataVersion = "1.0",
                };

            // Arrange Mocks
            Mock.Get(_storageService)
               .Setup(x => x.ContainerCreateAsync(createContainerData.StorageAccountName, createContainerData.ContainerName, It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(new InvalidOperationException());

            // Act
            var handleAsyncResult = await _handler.HandleAsync(testEvent).ConfigureAwait(true);

            // Assert
            handleAsyncResult.ShouldBe(false);
            Uri appInsightUri;
            Mock.Get(_logger).Verify(x =>
                    x.LogExceptionObject(out appInsightUri, LogEventIds.GridwichUnhandledException, It.IsAny<Exception>(), It.IsAny<object>()),
                Times.Once);
        }


        [Fact]
        public async Task HandleAsync_ShouldThrow_WhenInvalidEvent()
        {
            // Arrange
            var createContainerData = new RequestContainerCreateDTO { StorageAccountName = _storageAccountName, ContainerName = "unittestcontainer" + Guid.NewGuid().ToString() };
            var blobContainerCreateId = Guid.NewGuid().ToString();
            var testEvent =
                new EventGridEvent
                {
                    Data = JObject.FromObject(createContainerData),
                    EventType = CustomEventTypes.RequestCreateMetadata,
                    DataVersion = "1.0",
                };

            // // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _handler.HandleAsync(testEvent).ConfigureAwait(true)).ConfigureAwait(true);
        }
    }
}
