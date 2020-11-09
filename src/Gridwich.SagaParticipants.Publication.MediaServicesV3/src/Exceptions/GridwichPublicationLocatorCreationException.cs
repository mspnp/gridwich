using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationLocatorCreationException"/>.
    /// </summary>
    public class GridwichPublicationLocatorCreationException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationLocatorCreationException"/> class.
        /// </summary>
        /// <param name="assetName">The asset name for the locator.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationLocatorCreationException(string assetName, string message, Exception innerException)
         : base(message, LogEventIds.FailedToCreateLocator, null, innerException)
        {
            SafeAddToData("assetName", assetName);
        }
    }
}
