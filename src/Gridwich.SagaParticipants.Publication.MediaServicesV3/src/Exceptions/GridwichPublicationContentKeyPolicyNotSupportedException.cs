using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationContentKeyPolicyNotSupportedException"/>.
    /// </summary>
    public class GridwichPublicationContentKeyPolicyNotSupportedException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationContentKeyPolicyNotSupportedException"/> class.
        /// </summary>
        /// <param name="contentKeyPolicy">The requested contentKeyPolicy that which is not supported.</param>
        /// <param name="message">The base exception message you want to set.</param>
        public GridwichPublicationContentKeyPolicyNotSupportedException(string contentKeyPolicy, string message)
         : base(message, LogEventIds.PublicationContentKeyPolicyNotSupported, null)
        {
            SafeAddToData("contentKeyPolicy", contentKeyPolicy);
        }
    }
}
