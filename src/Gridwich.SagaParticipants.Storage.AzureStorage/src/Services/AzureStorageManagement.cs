using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Microsoft.Azure.Management.Fluent;
using System;
using System.Linq;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Services
{
    /// <summary>
    /// Azure Management Apis to do various things, like return account keys.
    /// </summary>
    public class AzureStorageManagement : IAzureStorageManagement
    {
        private readonly IAzure _azure;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageManagement"/> class.
        /// </summary>
        /// <param name="azure">An IAzure interface.</param>
        public AzureStorageManagement(IAzure azure)
        {
            _azure = azure;
        }

        /// <summary>
        /// Retrieves and account key for MSI managed storage accounts.
        /// </summary>
        /// <param name="accountName">The name of the account.</param>
        /// <returns>An account key for the specified account. <c>null</c> if none is found</returns>
        public string GetAccountKey(string accountName)
        {
            var storageAccounts = _azure.StorageAccounts.List();
            var accountKeys = storageAccounts
                .FirstOrDefault(sa => sa.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase))?
                .GetKeys();

            // always pull primary key as it allows for key-rolling mechanism
            var accountKey = accountKeys?.FirstOrDefault(k => k.KeyName.Equals(@"key1", StringComparison.OrdinalIgnoreCase))?.Value;

            return accountKey;
        }
    }
}
