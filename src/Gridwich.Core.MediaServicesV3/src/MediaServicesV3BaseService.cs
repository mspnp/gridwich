using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3.Exceptions;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Gridwich.Core.MediaServicesV3
{
    /// <inheritdoc/>
    public class MediaServicesV3BaseService : IMediaServicesV3BaseService
    {
        private bool clientIsConnected = false;

        /// <summary>
        /// Gets the log object.
        /// </summary>
        protected internal IObjectLogger<MediaServicesV3BaseService> Log { get; }

        /// <summary>
        /// Gets the MediaServicesV3SdkWrapper.
        /// </summary>
        protected internal IMediaServicesV3SdkWrapper MediaServicesV3SdkWrapper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3BaseService"/> class.
        /// </summary>
        /// <param name="mediaServicesV3SdkWrapper">Media Services V3 Wrapper object.</param>
        /// <param name="log">Logger.</param>
        public MediaServicesV3BaseService(
            IMediaServicesV3SdkWrapper mediaServicesV3SdkWrapper,
            IObjectLogger<MediaServicesV3BaseService> log)
        {
            Log = log;
            MediaServicesV3SdkWrapper = mediaServicesV3SdkWrapper;
        }

        /// <summary>
        /// Called by every service operation which requires the sdk wrapper to be connected.
        /// </summary>
        /// <returns>Return is a success when it does not throw.</returns>
        protected internal async Task ConnectAsync()
        {
            if (!clientIsConnected)
            {
                var id = LogEventIds.MediaServicesV3ConnectionError;
                Exception innerException = null;
                try
                {
                    await MediaServicesV3SdkWrapper.ConnectAsync().ConfigureAwait(false);
                    clientIsConnected = true;
                }
                catch (AdalServiceException ase)
                {
                    id = LogEventIds.MediaServicesV3ConnectionAdalError;
                    innerException = ase;
                }
                catch (Exception e)
                {
                    id = LogEventIds.MediaServicesV3ConnectionError;
                    innerException = e;
                }

                if (innerException != null)
                {
                    Log.LogException(id, innerException, id.Name);

                    var exceptionToThrow = new GridwichMediaServicesV3ConnectivityException(
                        id.Name,
                        id,
                        innerException);

                    throw exceptionToThrow;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<string> CreateOrUpdateAssetForContainerAsync(IEnumerable<Uri> videoSourceBlobUri)
        {
            await ConnectAsync().ConfigureAwait(false);

            if (videoSourceBlobUri == null)
            {
                throw new ArgumentNullException(nameof(videoSourceBlobUri));
            }

            // asset-{storageAccountName}-{containerName}
            if (!videoSourceBlobUri.Any())
            {
                throw new Exception("No blob Uris.");
            }

            videoSourceBlobUri = videoSourceBlobUri.ToList();   // we're using this IEnumerable for lots of stuff; so immediately .ToList() so we evaluate any incoming LINQ query now and don't keep doing it over and over as we LINQ the list in here

            // let's retrieve the storage account names and check they are all the same
            var blobStorageAccountNames = videoSourceBlobUri
                .Select(bs => new BlobUriBuilder(bs).AccountName)
                .ToList();  // since we're going to use this result multiple times, ToList() to save re-evaluation later
            if (blobStorageAccountNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != 1)
            {
                throw new Exception("All Uri are not in the same storage account.");
            }

            var blobStorageAccountName = blobStorageAccountNames.First();

            // let's compare with attached storage account of the media services account
            var amsaccount = await MediaServicesV3SdkWrapper.MediaservicesGetAsync().ConfigureAwait(false);
            var containsAmsStorageAccount = amsaccount.StorageAccounts
                .Select(s => s.Id.Split('/')
                    .Last())
                .Any(i => i.Equals(blobStorageAccountName, StringComparison.OrdinalIgnoreCase));
            if (!containsAmsStorageAccount)
            {
                throw new Exception("Uri(s) does not use a storage account attached to Media Services.");
            }

            // let's retrieve the container names and check they are all the same
            var blobStorageContainerNames = videoSourceBlobUri
                .Select(bs => new BlobUriBuilder(bs).BlobContainerName)
                .Distinct()
                .ToList();  // since we're going to use this result multiple times, ToList() to save re-evaluation later
            if (blobStorageContainerNames.Count != 1)
            {
                throw new Exception("All Uri are not in the same container.");
            }

            var containerName = blobStorageContainerNames.First().ToLowerInvariant();
            var storageName = blobStorageAccountName.ToLowerInvariant();
            var assetName = $"{storageName}-{containerName}";

            Asset newAsset = null;
            try
            {
                // Let's see if the asset exists already
                newAsset = await MediaServicesV3SdkWrapper.AssetGetAsync(assetName).ConfigureAwait(false);
            }
            catch
            {
                // if asset does not exist, newAsset is null but no exception thrown.
                // if exception is thrown, it means there is a critical issue.
                throw;
            }

            if (newAsset == null)
            {
                try
                {
                    // Create an input asset from the existing blob container location:
                    newAsset = await MediaServicesV3SdkWrapper.AssetCreateOrUpdateAsync(
                       assetName,
                       new Asset
                       {
                           StorageAccountName = storageName,
                           Container = containerName
                       }).ConfigureAwait(false);
                }
                catch
                {
                    throw;
                }
            }
            return newAsset.Name;
        }
    }
}