using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.MediaServicesV3
{
    /// <summary>
    /// Azure Media Services V3 base class for services.
    /// </summary>
    public interface IMediaServicesV3BaseService
    {
        /// <summary>
        /// Create an Azure Media Services v3 asset. It does not copy the blobs but create an asset from the container where are the blobs.
        /// Only the listed blobs should be in the container otherwise the method will fail. All listed blobs should be in the same container.
        /// </summary>
        /// <param name="videoSourceBlobUri">List of blobs which must be in the same storage container.</param>
        /// <returns>The created asset name.</returns>
        public Task<string> CreateOrUpdateAssetForContainerAsync(IEnumerable<Uri> videoSourceBlobUri);
    }
}