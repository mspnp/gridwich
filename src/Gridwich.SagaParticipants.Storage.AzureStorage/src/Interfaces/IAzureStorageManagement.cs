using System.Threading.Tasks;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces
{
    /// <summary>
    /// Azure Management storage related interfaces.
    /// </summary>
    public interface IAzureStorageManagement
    {
        /// <summary>
        /// Gets the account keys from Azure Management services.
        /// </summary>
        /// <param name="accountName">The name of the storage account.</param>
        /// <returns>An account key for the specified storage account.</returns>
        string GetAccountKey(string accountName);
    }
}