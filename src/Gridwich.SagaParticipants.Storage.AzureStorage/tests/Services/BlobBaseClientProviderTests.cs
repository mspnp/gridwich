using System;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
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
    public class BlobBaseClientProviderTests
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IObjectLogger<BlobBaseClientProvider> _logger;
        private readonly TokenCredential _identity;
        private readonly BlobBaseClientProvider _blobBaseClientProvider;
        private readonly IAppCache _blobBaseClientCache;

        public BlobBaseClientProviderTests()
        {
            _settingsProvider = Mock.Of<ISettingsProvider>();
            _logger = Mock.Of<IObjectLogger<BlobBaseClientProvider>>();
            _identity = Mock.Of<TokenCredential>();
            _blobBaseClientCache = new CachingService();
            _blobBaseClientProvider = new BlobBaseClientProvider(_settingsProvider, _blobBaseClientCache, _logger, _identity);
        }

        [Fact]
        public void GetBlobBaseClientForUri_ShouldThrow_WhenNullUri()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _blobBaseClientProvider.GetBlobClientSleeveForUri(null, StorageClientProviderContext.None));
        }

        [Fact]
        public void GetBlobBaseClientForUriWithRequestId_ShouldThrow_WhenNullUri()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(()
                => _blobBaseClientProvider.GetBlobClientSleeveForUri(null, StorageClientProviderContext.None));
        }

        [Fact]
        public void GetBlobBaseClientForUriWithRequestId_ShouldThrow_WhenNullContext()
        {
            // Arrange
            Uri uri = new Uri("https://gridwichasset00sasb.blob.core.windows.net/container1/blob1.mp4");

            // Act & Assert
            StorageClientProviderContext ctx = null;
            Assert.Throws<ArgumentNullException>(() => _blobBaseClientProvider.GetBlobClientSleeveForUri(uri, ctx));
        }

        [Fact]
        public void GetBlobBaseClientForUri_ShouldThrow_WhenUnknownStorageAccount()
        {
            // Arrange
            Uri uri1 = new Uri("https://gridwichasset00sasbblob.core.windows.net/container1/blob1.mp4");

            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, StorageClientProviderContext.None));
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.BlobBaseClientProviderUriMissingAccountName, It.IsAny<UriFormatException>(),
                    It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the storage account is unknown.");
        }

        [Fact(DisplayName = "Throw if malformed storage URL -- missing period before blob.core.*")]
        public void GetBlobBaseClientForUriWithId_ShouldThrow_WhenUnknownStorageAccount()
        {
            // Arrange
            Uri uri1 = new Uri("https://gridwichasset00sasbblob.core.windows.net/container1/blob1.mp4");

            // Act & Assert
            var ctx = StorageClientProviderContext.None;
            Assert.Throws<ArgumentException>(() => _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, ctx));
            Mock.Get(_logger).Verify(x =>
                x.LogExceptionObject(LogEventIds.BlobBaseClientProviderUriMissingAccountName, It.IsAny<UriFormatException>(),
                    It.IsAny<object>()),
                Times.Once,
                "An exception should be logged when the storage account is unknown.");
        }

        [Fact]
        public void GetBlobBaseClientForUri_ShouldReturnValidClient_WhenCalledAsExpected()
        {
            // Arrange
            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "core.windows.net";
            var expectedContainer = "container1";
            var expectedBlob = "path1/blob1.mp4";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.blob.{expectedEndpointSuffix}/{expectedContainer}/{expectedBlob}");
            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1));

            // Act
            var newClientSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, StorageClientProviderContext.None);
            var newClient = newClientSleeve.Client;

            // Assert
            newClient.AccountName.ShouldBe(expectedAccount);
            newClient.BlobContainerName.ShouldBe(expectedContainer);
            newClient.Name.ShouldBe(expectedBlob);
            newClient.Uri.ShouldBe(uri1);
            newClient.ShouldBeOfType<BlobBaseClient>();
        }

        [Fact]
        public void GetBlobBaseClientForUriWithId_ShouldReturnValidClient_WhenCalledAsExpected()
        {
            // Arrange
            var clientRequestId = "{\"somehappykey\" : \"somehappyvalue\"}";
            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "core.windows.net";
            var expectedContainer = "container1";
            var expectedBlob = "path1/blob1.mp4";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.blob.{expectedEndpointSuffix}/{expectedContainer}/{expectedBlob}");
            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1));

            // Act
            var ctx = new StorageClientProviderContext(clientRequestId, false, string.Empty);
            var newClientSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, ctx);
            var newClient = newClientSleeve.Client;

            // Assert
            newClient.AccountName.ShouldBe(expectedAccount);
            newClient.BlobContainerName.ShouldBe(expectedContainer);
            newClient.Name.ShouldBe(expectedBlob);
            newClient.Uri.ShouldBe(uri1);
            newClient.ShouldBeOfType<BlobBaseClient>();
        }

        /// <summary>
        /// Check that sleeves already in cache come back as themselves and ones that aren't in cache are different.
        /// </summary>
        [Fact]
        public void GetBlobBaseClientForUri_ShouldReturnNewClient_WhenNotInDictionary()
        {
            // Arrange
            var context = new StorageClientProviderContext("{ \"something\" : \"good\" }", false, string.Empty);

            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "core.windows.net";
            var expectedContainer = "container1";
            var expectedBlob = "path1/blob1.mp4";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.{expectedEndpointSuffix}/{expectedContainer}/{expectedBlob}");
            Uri uri2 = new Uri("https://gridwichasset00sasb.blob.core.windows.net/container2/blob2.mp4");

            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context));
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri2, context));

            var blobBaseClient = Mock.Of<BlobBaseClient>();
            var sleeve = MockSleeve(blobBaseClient, context);

            _blobBaseClientCache.Add(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context), sleeve);

            // Act
            var existingClientSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, context);
            var existingClient = existingClientSleeve.Client;
            var newClientSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri2, context);
            var newClient = newClientSleeve.Client;

            // Assert

            // So existing should match original and new shouldn't.
            existingClientSleeve.ShouldNotBe(newClientSleeve);
            existingClientSleeve.ShouldBe(sleeve);
            newClient.ShouldBeOfType<BlobBaseClient>();
            newClient.ShouldNotBeSameAs(sleeve.Client);
            existingClient.ShouldBe(sleeve.Client);
            newClient.ShouldNotBe(existingClient);
        }

        [Fact]
        public void GetBlobBaseClientForUriWithId_ShouldReturnNewClient_WhenNotInDictionary()
        {
            // Arrange
            var context1 = new StorageClientProviderContext("{\"somehappykey\" : \"somehappyvalue\"}", false, string.Empty);
            var context2 = new StorageClientProviderContext("{\"anotherhappykey\" : \"anotherhappyvalue\"}", false, string.Empty);

            var expectedScheme = "https";
            var expectedAccount = "gridwichasset00sasb";
            var expectedEndpointSuffix = "core.windows.net";
            var expectedContainer = "container1";
            var expectedBlob = "path1/blob1.mp4";
            Uri uri1 = new Uri($"{expectedScheme}://{expectedAccount}.{expectedEndpointSuffix}/{expectedContainer}/{expectedBlob}");
            Uri uri2 = new Uri("https://gridwichasset00sasb.blob.core.windows.net/container2/blob2.mp4");

            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context1));
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri2, context2));

            var blobBaseClient = Mock.Of<BlobBaseClient>();
            var sleeve1 = MockSleeve(blobBaseClient, context1);
            _blobBaseClientCache.Add(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context1), sleeve1);

            // Act
            var existingClientSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, context1);
            var existingClient = existingClientSleeve.Client;
            var newClientSleeve = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri2, context2);
            var newClient = newClientSleeve.Client;

            // Assert
            existingClient.ShouldBeSameAs(blobBaseClient);
            newClient.ShouldNotBeSameAs(blobBaseClient);

            existingClientSleeve.Context.ShouldBeEquivalentTo(context1);
            newClientSleeve.Context.ShouldBeEquivalentTo(context2);
        }

        /// <summary>
        /// Testing that a client that has no context (more specifically the "empty" context)
        /// is retrieved properly.
        /// </summary>
        [Fact]
        public void GetBlobBaseClientForUri_ShouldReturnExistingClient_WhenAlreadyInDictionary()
        {
            // Arrange
            Uri uri1 = new Uri("https://gridwichasset00sasb.blob.core.windows.net/container1/blob1.mp4");
            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1));
            var blobBaseClient = Mock.Of<BlobBaseClient>();
            var context = StorageClientProviderContext.None;
            var sleeve = MockSleeve(blobBaseClient, context);

            _blobBaseClientCache.Add(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context), sleeve);

            // Act
            var result = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, context);

            // Assert
            result.Client.ShouldBe(blobBaseClient);
        }

        [Fact]
        public void GetBlobBaseClientForUriWithId_ShouldReturnExistingClient_WhenAlreadyInDictionary()
        {
            // Arrange
            var context1 = new StorageClientProviderContext("{\"somehappykey\" : \"somehappyvalue\"}", false, string.Empty);

            Uri uri1 = new Uri("https://gridwichasset00sasb.blob.core.windows.net/container1/blob1.mp4");

            // Ensure we don't pick up a cached object:
            _blobBaseClientCache.Remove(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context1));

            var blobSleeve = MockSleeve(Mock.Of<BlobBaseClient>(), context1);

            _blobBaseClientCache.Add(BlobBaseClientProvider.GetCacheKeyForBlobBaseClient(uri1, context1), blobSleeve);

            // Act
            var result = _blobBaseClientProvider.GetBlobClientSleeveForUri(uri1, context1);

            // Assert
            result.Client.ShouldBe(blobSleeve.Client);
            result.Context.ShouldBe(blobSleeve.Context);
        }

        [Fact]
        public void StorageBlobClientSleeve_Constructor()
        {
            // Arrange
            var client = Mock.Of<BlobBaseClient>();
            Mock.Get(client)
                .SetupGet(x => x.AccountName)
                .Returns("gridwichinbox00sasb");
            var svcClient = Mock.Of<BlobServiceClient>();
            var context = TestHelpers.CreateGUIDContext();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new StorageBlobClientSleeve(client, svcClient, null));
            Assert.Throws<ArgumentNullException>(() => new StorageBlobClientSleeve(client, null, context));
            Assert.Throws<ArgumentNullException>(() => new StorageBlobClientSleeve(null, svcClient, context));
            // Should not fail
            var sleeve = new StorageBlobClientSleeve(client, svcClient, context);
        }

        /// <summary>
        /// Create a mock sleeve, properly set up so that the two main getter's work.
        /// </summary>
        private static IStorageBlobClientSleeve MockSleeve(BlobBaseClient client, StorageClientProviderContext context)
        {
            Mock.Get(client)
                .SetupGet(x => x.AccountName)
                .Returns(@"ut");

            var serviceClient = new BlobServiceClient(new Uri($@"https://{client.AccountName}.blob.core.windows.net"));

            var sleeve = Mock.Of<IStorageBlobClientSleeve>();
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
