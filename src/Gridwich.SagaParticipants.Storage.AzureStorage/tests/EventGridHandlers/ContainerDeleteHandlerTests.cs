using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.EventGridHandlers
{
    public class ContainerDeleteHandlerTests
    {
        private readonly ContainerDeleteHandler _handler;
        private readonly IObjectLogger<ContainerDeleteHandler> _logger;
        private readonly IStorageService _storageService;
        private readonly IEventGridPublisher _eventGridPublisher;

        public ContainerDeleteHandlerTests()
        {
            _eventGridPublisher = Mock.Of<IEventGridPublisher>();
            _storageService = Mock.Of<IStorageService>();
            _logger = Mock.Of<IObjectLogger<ContainerDeleteHandler>>();
            _handler = new ContainerDeleteHandler(
                _logger,
                _storageService,
                _eventGridPublisher);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotFail()
        {
            // Arrange
            var deleteContainerData = new RequestContainerDeleteDTO { StorageAccountName = "account", ContainerName = "unittestcontainer" + Guid.NewGuid().ToString() };
            var testEvent =
                new EventGridEvent
                {
                    Data = JObject.FromObject(deleteContainerData),
                    EventType = CustomEventTypes.RequestBlobContainerDelete,
                    DataVersion = "1.0",
                };
            Mock.Get(_storageService)
                .Setup(x => x.ContainerDeleteAsync(deleteContainerData.StorageAccountName, deleteContainerData.ContainerName, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);

            // Act
            var resultEvent = await _handler.HandleAsync(testEvent).ConfigureAwait(false);

            // Assert
            resultEvent.ShouldBeFalse();
        }
    }
}