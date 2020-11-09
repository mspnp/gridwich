using System.Collections.Generic;
using Gridwich.SagaParticipants.Storage.AzureStorage.Services;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Moq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.Services
{
    public class AzureStorageManagementTests
    {
        private readonly IAzure _azure = Mock.Of<IAzure>();

        [Fact]
        public void AzureStorageManagement_GetAccountKeys()
        {
            // Arrange Mocks
            const string validAccountName = "validname";

            var sacts = Mock.Of<IStorageAccounts>();
            var sa = Mock.Of<IStorageAccount>();

            Mock.Get(sa)
                .Setup(x => x.GetKeys())
                .Returns(new List<StorageAccountKey>
                {
                    new StorageAccountKey(@"key1", @"unitTestKey") // primary key from Azure is named 'key1'
                });

            Mock.Get(sa)
                .SetupGet(x => x.Name)
                .Returns(validAccountName);

            Mock.Get(sacts)
                .Setup(x => x.List())
                .Returns(new[] { sa });

            Mock.Get(_azure)
            .Setup(x => x.StorageAccounts)
            .Returns(sacts);

            // Act
            var azureStorageManagement = new AzureStorageManagement(_azure);
            var accountKey = azureStorageManagement.GetAccountKey(validAccountName);

            // Assert
            accountKey.ShouldBe(@"unitTestKey");
        }
    }
}
