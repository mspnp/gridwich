using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationListPathsException"/>.
    /// </summary>
    public class GridwichPublicationListPathsException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationListPathsException"/> class.
        /// </summary>
        /// <param name="locatorName">The requested locatorName which was not found.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationListPathsException(string locatorName, string message, Exception innerException)
         : base(message, LogEventIds.FailedToListStreamingPaths, null, innerException)
        {
            SafeAddToData("locatorName", locatorName);
        }
    }
}
