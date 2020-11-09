using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationLocatorDeletionException"/>.
    /// </summary>
    public class GridwichPublicationLocatorDeletionException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationLocatorDeletionException"/> class.
        /// </summary>
        /// <param name="locatorName">The requested locatorName.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationLocatorDeletionException(string locatorName, string message, Exception innerException)
         : base(message, LogEventIds.FailedToDeleteLocator, null, innerException)
        {
            SafeAddToData("locatorName", locatorName);
        }
    }
}
