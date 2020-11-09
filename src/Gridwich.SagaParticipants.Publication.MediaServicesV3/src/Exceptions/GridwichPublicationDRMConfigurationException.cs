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
    public class GridwichPublicationDRMConfigurationException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationDRMConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationDRMConfigurationException(string message, Exception innerException)
         : base(message, LogEventIds.MediaServicesV3ConfigurationError, null, innerException)
        {
        }
    }
}
