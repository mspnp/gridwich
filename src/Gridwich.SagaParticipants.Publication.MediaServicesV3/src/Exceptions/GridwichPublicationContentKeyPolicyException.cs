using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationContentKeyPolicyException"/>.
    /// </summary>
    public class GridwichPublicationContentKeyPolicyException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationContentKeyPolicyException"/> class.
        /// </summary>
        /// <param name="contentKeyPolicyName">The content key policy name to be created.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichPublicationContentKeyPolicyException(string contentKeyPolicyName, string message, Exception innerException)
         : base(message, LogEventIds.MediaServicesV3ContentKeyPolicyCreateUpdateError, null, innerException)
        {
            SafeAddToData("contentKeyPolicyName", contentKeyPolicyName);
        }
    }
}
