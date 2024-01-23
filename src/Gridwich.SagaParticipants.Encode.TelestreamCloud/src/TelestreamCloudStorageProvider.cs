using Azure.Storage.Blobs;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Helpers;
using Gridwich.SagaParticipants.Encode.TelestreamCloud.Exceptions;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telestream.Cloud.Stores.Client;
using Telestream.Cloud.Stores.Model;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloud
{
    /// <summary>
    /// Providers Telestream storage reference configuration for Flip and CloudPort encoders.
    /// </summary>
    ///
    public class TelestreamCloudStorageProvider : ITelestreamCloudStorageProvider
    {
        private const string GridwichStoragePrefix = @"GRIDWICH-";
        private readonly ITelestreamCloudClientProvider _telestreamCloudClientProvider;
        private readonly IAzureStorageManagement _azureStorageManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelestreamCloudStorageProvider" /> class.
        /// </summary>
        /// <param name="telestreamCloudClientProvider">The telestream cloud client provider.</param>
        /// <param name="azureStorageManagement">The Azure Storage Management service.</param>
        public TelestreamCloudStorageProvider(ITelestreamCloudClientProvider telestreamCloudClientProvider, IAzureStorageManagement azureStorageManagement)
        {
            _telestreamCloudClientProvider = telestreamCloudClientProvider;
            _azureStorageManagement = azureStorageManagement;
        }

        /// <summary>
        /// Deletes all Gridwich storage references in Telestream Cloud account.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="GridwichTelestreamCloudApiException">Thrown when Telestream Cloud storage processes invalid input.</exception>
        public async Task DeleteGridwichStorageReferencesAsync()
        {
            try
            {
                var stores = await _telestreamCloudClientProvider.CloudPortStoresApi.ListStoresAsync().ConfigureAwait(false);
                var gridwichStores = stores.Where(x => x.Name.StartsWith(GridwichStoragePrefix));
                foreach (var store in gridwichStores.OrEmptyIfNull())
                {
                    await _telestreamCloudClientProvider.CloudPortStoresApi.DeleteStoreAsync(store.Id).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                throw new GridwichTelestreamCloudApiException("Error deleting Telestream Cloud storage references.", LogEventIds.CloudPortDeletingStorageReferencesError, null, e);
            }
        }

        /// <summary>
        /// Given a Uri, gets or creates a Telestream Cloud Store object.
        /// </summary>
        /// <param name="containerUri">A full blob Url.</param>
        /// <returns>
        /// A Store object.
        /// </returns>
        /// <exception cref="ArgumentException">containerUri</exception>
        public async Task<Store> GetStoreByNameAsync(Uri containerUri)
        {
            var blobUriBuilder = new BlobUriBuilder(containerUri);

            // Establish a convention that the Vantage cloud reference name is the
            // same as the Azure account + blob container name, prefixed with a "GRIDWICH-".
            var cloudPortStorageRefName = GridwichStoragePrefix + blobUriBuilder.AccountName + "-" + blobUriBuilder.BlobContainerName;

            var stores = await _telestreamCloudClientProvider.CloudPortStoresApi.ListStoresAsync().ConfigureAwait(false);
            var store = stores.FirstOrDefault(w => w.Name?.Equals(cloudPortStorageRefName, StringComparison.OrdinalIgnoreCase) == true);

            if (store is default(Store))
            {
                try
                {
                    var accountKey = _azureStorageManagement.GetAccountKey(blobUriBuilder.AccountName);
                    if (string.IsNullOrWhiteSpace(accountKey))
                    {
                        throw new GridwichTelestreamCloudApiException($@"Unable to find account key for storage account '{blobUriBuilder.AccountName}'", LogEventIds.CloudPortCannotFindStorageAccountKeyError, null);
                    }

                    var newStore = new Store
                    {
                        Name = cloudPortStorageRefName,
                        Provider = "ABS",
                        BucketName = blobUriBuilder.BlobContainerName,
                        // These next two seem awkwardly named, but, for legacy reasons, the CloudPort API works this way.
                        AccessKey = blobUriBuilder.AccountName,
                        SecretKey = accountKey
                    };

                    store = await _telestreamCloudClientProvider.CloudPortStoresApi.CreateStoreAsync(newStore).ConfigureAwait(false);
                }
                catch (ApiException ae)
                {
                    throw new GridwichTelestreamCloudApiException("Api error creating Telestream storage reference.  Potentially bad connections string.", LogEventIds.CloudPortCannotCreateStorageReferenceAPIError, null, ae);
                }
                catch (GridwichException)
                {
                    // Just re-throw any application exceptions that bubble up
                    throw;
                }
                catch (Exception e)
                {
                    throw new GridwichTelestreamCloudApiException("Unknown error creating Telestream storage reference.", LogEventIds.CloudPortCannotCreateStorageReferenceUnknownError, null, e);
                }
            }

            return store;
        }
    }
}
