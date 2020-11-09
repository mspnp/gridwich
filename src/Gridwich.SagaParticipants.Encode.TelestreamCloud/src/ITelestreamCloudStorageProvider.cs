using System;
using System.Threading.Tasks;
using Telestream.Cloud.Stores.Model;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloud
{
    /// <summary>
    /// Provides services using the Telestream Cloud storage API.
    /// </summary>
    public interface ITelestreamCloudStorageProvider
    {
        /// <summary>
        /// Gets or creates a Store object from Telestream Cloud, given a container url.
        /// </summary>
        /// <param name="containerUri">The uri for the container.</param>
        /// <returns>
        /// A Store object.
        /// </returns>
        Task<Store> GetStoreByNameAsync(Uri containerUri);

        /// <summary>
        /// Deletes all Telestream Cloud storage references used by Gridwich.
        /// </summary>
        /// <returns>
        ///   <see cref="Task"/>
        /// </returns>
        Task DeleteGridwichStorageReferencesAsync();
    }
}
