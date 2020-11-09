using System;
using Azure.Core;
using Azure.Storage.Blobs;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Gridwich.SagaParticipants.Storage.AzureStorage.Services;
using LazyCache;

using Moq;

using Shouldly;

using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.Services
{
    public class BlobContainerClientProviderTests
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IObjectLogger<BlobContainerClientProvider> _logger;
        private readonly TokenCredential _identity;
        private readonly BlobContainerClientProvider _blobContainerClientProvider;
        private readonly IAppCache _blobContainerClientCache;

        public BlobContainerClientProviderTests()
        {
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobContainerClientProvider>>();
            _identity = Mock.Of<TokenCredential>();
            _blobContainerClientCache = new CachingService();
            _blobContainerClientProvider = new Mock<BlobContainerClientProvider>(_settingsProvider, _blobContainerClientCache, _logger, _identity).Object;
        }

        [Fact]
        public void GetBlobContainerClientForUri_ShouldThrow_WhenNullUri()
        {
            var context = new StorageClientProviderContext("{ \"something\" : \"good\" }", false, string.Empty);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => _blobContainerClientProvider.GetBlobContainerSleeveForUri(null, context));
        }

        [Fact(DisplayName = "Throw when a malformed Storage URL, e.g., no blob.core.windows.net suffix")]
        public void GetBlobContainerClientForUri_ShouldThrow_WhenUnknownStorageAccount()
        {
            // Arrange
            Uri uri1 = new Uri("https://gridwichasset00sasb.com/container1");
            var context = new StorageClientProviderContext("{ \"something\" : \"good\" }", false, string.Empty);

            // Ensure we don't pick up a cached object:
            _blobContainerClientCache.Remove(BlobContainerClientProvider.GetCacheKeyForBlobContainerClient(uri1, context));

            // Act & Assert
            Assert.ThrowsAny<ArgumentException>(() => _blobContainerClientProvider.GetBlobContainerSleeveForUri(uri1, context));
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.BlobContainerClientProviderUriMissingAccountName, It.IsAny<ArgumentException>(),
                    It.IsAny<object>()),
                Times.Once,
                "A critical error should be logged when the storage account is unknown.");
        }

        [Fact]
        public void GetBlobContainerClientForUri_ShouldReturnValidClient_WhenCalledAsExpected()
        {
            // Arrange
            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "blob.core.windows.net";
            var expectedContainer = "container1";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.{expectedEndpointSuffix}/{expectedContainer}");
            var context = new StorageClientProviderContext("{ \"something\" : \"good\" }", false, string.Empty);
            // Ensure we don't pick up a cached object:
            _blobContainerClientCache.Remove(BlobContainerClientProvider.GetCacheKeyForBlobContainerClient(uri1, context));

            // Act
            var newSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(uri1, context);
            var newClient = newSleeve.Client;

            // Assert
            newClient.AccountName.ShouldBe(expectedAccount);
            newClient.Name.ShouldBe(expectedContainer);
            newClient.Uri.ShouldBe(uri1);
            newClient.ShouldBeOfType<BlobContainerClient>();
        }

        [Fact]
        public void GetBlobContainerClientForUri_ShouldReturnNewClient_WhenNotInDictionary()
        {
            // Arrange
            var context = new StorageClientProviderContext("{ \"something\" : \"good\" }", false, string.Empty);
            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "blob.core.windows.net";
            var expectedContainer = "container1";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.{expectedEndpointSuffix}/{expectedContainer}");
            Uri uri2 = new Uri($"{expectedScheme}://{expectedAccount}.{expectedEndpointSuffix}/container2");

            var key1 = BlobContainerClientProvider.GetCacheKeyForBlobContainerClient(uri1, context);
            var key2 = BlobContainerClientProvider.GetCacheKeyForBlobContainerClient(uri2, context);

            // Ensure we don't pick up a cached object:
            _blobContainerClientCache.Remove(key1);
            _blobContainerClientCache.Remove(key2);

            var blobContainerClient = Mock.Of<BlobContainerClient>();
            var sleeve = MockSleeve(blobContainerClient, context);
            _blobContainerClientCache.Add(key1, sleeve);

            // Act
            var existingSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(uri1, context);
            var newSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(uri2, context);

            // Assert

            // Existing should match original and new shouldn't.
            newSleeve.Client.ShouldBeOfType<BlobContainerClient>();
            newSleeve.Client.ShouldNotBeSameAs(blobContainerClient);
            newSleeve.Context.ShouldBeEquivalentTo(context);

            existingSleeve.Client.ShouldBeAssignableTo<BlobContainerClient>();
            existingSleeve.Client.ShouldBeSameAs(blobContainerClient);
            existingSleeve.Context.ShouldBeEquivalentTo(context);

            newSleeve.ShouldNotBe(sleeve);
            newSleeve.ShouldNotBe(existingSleeve);
            existingSleeve.ShouldNotBe(newSleeve);
            existingSleeve.ShouldBe(sleeve);
        }

        [Fact]
        public void GetBlobContainerClientForUri_ShouldReturnExistingClient_WhenAlreadyInDictionary()
        {
            // Arrange
            var context = new StorageClientProviderContext("{ \"something\" : \"else\" }", false, string.Empty);
            Uri uri1 = new Uri("https://gridwichasset00sasb.com/container1");
            var key1 = BlobContainerClientProvider.GetCacheKeyForBlobContainerClient(uri1, context);

            // Ensure we don't pick up a cached object:
            _blobContainerClientCache.Remove(key1);

            var blobContainerClient = Mock.Of<BlobContainerClient>();
            var sleeve = MockSleeve(blobContainerClient, context);
            _blobContainerClientCache.Add(key1, sleeve);

            // Act
            var resultSleeve = _blobContainerClientProvider.GetBlobContainerSleeveForUri(uri1, context);

            // Assert
            resultSleeve.Client.ShouldBeSameAs(blobContainerClient);
        }

        [Fact]
        public void StorageContainerClientSleeve_Constructor()
        {
            // Arrange
            var client = Mock.Of<BlobContainerClient>();
            Mock.Get(client)
                .SetupGet(x => x.AccountName)
                .Returns("gridwichinbox00sasb");
            var svcClient = Mock.Of<BlobServiceClient>();
            var context = TestHelpers.CreateGUIDContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StorageContainerClientSleeve(client, svcClient, null));
            Assert.Throws<ArgumentNullException>(() => new StorageContainerClientSleeve(client, null, context));
            Assert.Throws<ArgumentNullException>(() => new StorageContainerClientSleeve(null, svcClient, context));
            // Should not fail
            var sleeve = new StorageContainerClientSleeve(client, svcClient, context);
        }

        /// <summary>
        /// Create a mock sleeve, properly set up so that the two main getter's work.
        /// </summary>
        private static IStorageContainerClientSleeve MockSleeve(BlobContainerClient client, StorageClientProviderContext context)
        {
            Mock.Get(client)
                .SetupGet(x => x.AccountName)
                .Returns(@"ut");

            var sleeve = Mock.Of<IStorageContainerClientSleeve>();
            Mock.Get(sleeve)
                .SetupGet(x => x.Client)
                .Returns(client);
            Mock.Get(sleeve)
                .SetupGet(x => x.Context)
                .Returns(context);

            return sleeve;
        }
    }
}
