using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Gridwich.SagaParticipants.Storage.AzureStorage.Exceptions;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class ContainerAccessChangeHandlerTests
    {
        private readonly ContainerAccessChangeHandler _handler;
        private readonly IObjectLogger<ContainerAccessChangeHandler> _logger;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _publisher;

        public ContainerAccessChangeHandlerTests()
        {
            _publisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _logger = Mock.Of<IObjectLogger<ContainerAccessChangeHandler>>();
            _handler = new ContainerAccessChangeHandler(
                _logger,
                _storageService,
                _publisher);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotFail()
        {
            // Arrange
            var dto = new RequestContainerAccessChangeDTO
            {
                StorageAccountName = "account",
                ContainerName = "container",
                AccessType = ContainerAccessType.Blob
            };
            var testEvent =
                new EventGridEvent
                {
                    Data = JObject.FromObject(dto),
                    EventType = CustomEventTypes.RequestBlobContainerAccessChange,
                    DataVersion = "1.0",
                };
            Mock.Get(_storageService)
                .Setup(x => x.ContainerSetPublicAccessAsync(
                    dto.StorageAccountName,
                    dto.ContainerName,
                    dto.AccessType,
                    It.IsAny<StorageClientProviderContext>()));
            Mock.Get(_publisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.HandleAsync(testEvent).ConfigureAwait(false);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnGridwichFailure()
        {
            // Arrange
            var uri = new Uri("http://x.y/z");
            var dto = new RequestContainerAccessChangeDTO
            {
                StorageAccountName = "account",
                ContainerName = "container",
                AccessType = ContainerAccessType.Blob
            };
            var testEvent = new EventGridEvent
            {
                Data = JObject.FromObject(dto),
                EventType = CustomEventTypes.RequestBlobContainerAccessChange,
                DataVersion = "1.0",
            };
            var exception = new GridwichStorageServiceException("Error changing container access", LogEventIds.FailedToChangeContainerAccess, Mock.Of<JObject>());
            Mock.Get(_storageService)
                .Setup(x => x.ContainerSetPublicAccessAsync(
                    dto.StorageAccountName,
                    dto.ContainerName,
                    dto.AccessType,
                    It.IsAny<StorageClientProviderContext>()))
                .ThrowsAsync(exception);
            Mock.Get(_publisher)
                .Setup(x => x.PublishEventToTopic(It.IsAny<EventGridEvent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.HandleAsync(testEvent).ConfigureAwait(false);

            // Assert
            result.ShouldBeFalse();
            Mock.Get(_logger).Verify(
                x => x.LogExceptionObject(out uri, LogEventIds.FailedToChangeContainerAccess, exception, testEvent));
        }
    }
}