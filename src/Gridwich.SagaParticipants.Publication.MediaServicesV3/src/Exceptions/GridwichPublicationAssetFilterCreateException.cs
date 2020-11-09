using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationAssetFilterCreateException"/>.
    /// </summary>
    public class GridwichPublicationAssetFilterCreateException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationAssetFilterCreateException"/> class.
        /// </summary>
        /// <param name="assetName">The asset name on which the asset filter is created.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationAssetFilterCreateException(string assetName, string message, Exception innerException)
         : base(message, LogEventIds.FailedToCreateAssetFilter, null, innerException)
        {
            SafeAddToData("assetName", assetName);
        }
    }
}
