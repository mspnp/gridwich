using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.Helpers;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Requestor-specific message for the BlobTierChange event.
    /// </summary>
    public class RequestBlobTierChangeDTO : RequestBaseDTO
    {
        /// <summary>
        /// Gets or sets the target BlobUri.
        /// </summary>
        [property:SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification="Not applicable to DTOs")]
        [JsonProperty("blobUri")]
        public string BlobUri { get; set; }

        /// <summary>
        /// Gets or sets the target access tier.
        /// </summary>
        public BlobAccessTier AccessTier { get; set; }

        /// <summary>
        /// Gets or sets the priority of the rehydrate operation.
        /// </summary>
        [JsonProperty("rehydratePriority")]
        public BlobRehydratePriority RehydratePriority { get; set; } = StorageServiceConstants.DefaultRehydratePriority;
    }
}