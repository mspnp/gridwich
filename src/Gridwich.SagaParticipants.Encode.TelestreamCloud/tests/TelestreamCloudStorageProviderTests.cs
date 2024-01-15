using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Gridwich.SagaParticipants.Encode.TelestreamCloud.Exceptions;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telestream.Cloud.Stores.Api;
using Telestream.Cloud.Stores.Client;
using Telestream.Cloud.Stores.Model;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloudTests
{
    public class TelestreamCloudStorageProviderTests
    {
        // Arrange Mocks
        private readonly ITelestreamCloudClientProvider _telestreamCloudClientProvider = Mock.Of<ITelestreamCloudClientProvider>(c => c.CloudPortStoresApi == Mock.Of<IStoresApi>());

        // Arrange
        private readonly string storageRefName = "GRIDWICH-accountname-containername";
        private readonly Uri blobUri = new Uri("https://accountname.blob.core.windows.net/containername");
        private readonly ISettingsProvider _settings;
        private readonly IAzureStorageManagement _azureStorageManagement = Mock.Of<IAzureStorageManagement>();

        public TelestreamCloudStorageProviderTests()
        {
            _settings = Mock.Of<ISettingsProvider>();
            Mock.Get(_settings)
                .Setup(x => x.GetAppSettingsValue(@"KeyVaultBaseUrl"))
                .Returns(@"https://unitTest.vault.azure.net");

            Mock.Get(_azureStorageManagement)
                .Setup(x => x.GetAccountKey(It.IsAny<string>()))
                .Returns("notarealkey");
        }

        [Fact]
        public async Task StorageProvider_GetStoreByNameShouldSucceedWhenExists()
        {
            // Arrange Mocks
            var store1 = new Store
            {
                Name = storageRefName
            };
            var store2 = new Store();
            var stores = new List<Store> { store1, store2 };

            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Setup(x => x.ListStoresAsync(null, null))
                .ReturnsAsync(stores);

            // Act
            var telestreamCloudStorageProvider = new TelestreamCloudStorageProvider(_telestreamCloudClientProvider, Mock.Of<IAzureStorageManagement>());
            var store = await telestreamCloudStorageProvider.GetStoreByNameAsync(blobUri).ConfigureAwait(false);

            // Assert
            store.ShouldNotBeNull();
            store.Name.ShouldBe(storageRefName);
        }

        [Fact]
        public async Task StorageProvider_GetStoreByNameShouldSucceedWhenDoesNotExist()
        {
            // Arrange Mocks
            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Setup(x => x.ListStoresAsync(null, null))
                .ReturnsAsync(Enumerable.Repeat(new Store(), 2).ToList());

            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Setup(x => x.CreateStoreAsync(It.IsAny<Store>()))
                .ReturnsAsync(new Store
                {
                    Name = storageRefName
                });


            // Act
            var telestreamCloudStorageProvider = new TelestreamCloudStorageProvider(_telestreamCloudClientProvider, _azureStorageManagement);
            var store = await telestreamCloudStorageProvider.GetStoreByNameAsync(blobUri).ConfigureAwait(false);

            // Assert
            store.ShouldNotBeNull();
            store.Name.ShouldBe(storageRefName);
        }

        [Fact]
        public async Task DeleteStorageReferenceAsync_ShouldThrowApiException()
        {
            // Arrange Mocks
            var store1 = new Store { Name = storageRefName };
            var store2 = new Store { Name = "NotaGridwichStore" };
            var stores = new List<Store> { store1, store2 };

            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Setup(x => x.ListStoresAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(stores);

            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Setup(x => x.DeleteStoreAsync(It.IsAny<string>()))
                .Throws(new ApiException());

            // Act
            var telestreamCloudStorageProvider = new TelestreamCloudStorageProvider(_telestreamCloudClientProvider, _azureStorageManagement);
            var ex = await Record.ExceptionAsync(async () => await telestreamCloudStorageProvider.DeleteGridwichStorageReferencesAsync().ConfigureAwait(false)).ConfigureAwait(false);

            // Assert
            ex.ShouldBeOfType<GridwichTelestreamCloudApiException>();
        }

        [Fact]
        public async Task DeleteStorageReferenceAsync_ShouldCallDeleteOnce()
        {
            // Arrange Mocks
            var store1 = new Store { Name = storageRefName };
            var store2 = new Store { Name = "NotaGridwichStore" };
            var stores = new List<Store> { store1, store2 };

            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Setup(x => x.ListStoresAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(stores);

            var telestreamCloudStorageProvider = new TelestreamCloudStorageProvider(_telestreamCloudClientProvider, _azureStorageManagement);
            await telestreamCloudStorageProvider.DeleteGridwichStorageReferencesAsync().ConfigureAwait(false);

            Mock.Get(_telestreamCloudClientProvider.CloudPortStoresApi)
                .Verify(x => x.DeleteStoreAsync(It.IsAny<string>()), Times.Once,
                "Should delete one and only one store.");
        }
    }
}
