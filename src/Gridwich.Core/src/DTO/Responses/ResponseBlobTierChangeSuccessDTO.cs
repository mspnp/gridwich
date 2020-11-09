using Gridwich.Core.Constants;
using Newtonsoft.Json;

namespace Gridwich.Core.DTO
{
    /// <summary>
    /// This class provides a Gridwich-specific message for the RequestBlobTierChange event.
    /// </summary>
    public class ResponseBlobTierChangeSuccessDTO : ResponseBaseDTO
    {
        /// <summary>
        /// Gets or sets the target BlobUri.
        /// </summary>
        [property:System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification="Not applicable to DTOs")]
        [JsonProperty("blobUri")]
        public string BlobUri { get; set; }

        /// <summary>
        /// Gets or sets the target access tier.
        /// </summary>
        [JsonProperty("accessTier")]
        public BlobAccessTier AccessTier { get; set; }

        /// <summary>
        /// Gets or sets the priority of the rehydrate operation.
        /// </summary>
        [JsonProperty("rehydratePriority")]
        public BlobRehydratePriority RehydratePriority { get; set; } = StorageServiceConstants.DefaultRehydratePriority;
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBlobTierChangeSuccessDTO"/> class.
        /// </summary>
        public ResponseBlobTierChangeSuccessDTO()
            : base(CustomEventTypes.ResponseBlobTierChanged)
        {
        }
    }
}