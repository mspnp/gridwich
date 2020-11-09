using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationCreateUpdateAssetException"/>.
    /// </summary>
    public class GridwichPublicationCreateUpdateAssetException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationCreateUpdateAssetException"/> class.
        /// </summary>
        /// <param name="containerUri">The containerUri of the asset.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationCreateUpdateAssetException(Uri containerUri, string message, Exception innerException)
         : base(message, LogEventIds.FailedToCreateOrGetAsset, null, innerException)
        {
            SafeAddToData("containerUri", containerUri?.ToString());
        }
    }
}
