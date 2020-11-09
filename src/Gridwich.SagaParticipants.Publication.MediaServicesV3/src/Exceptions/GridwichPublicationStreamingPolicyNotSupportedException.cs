using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationStreamingPolicyNotSupportedException"/>.
    /// </summary>
    public class GridwichPublicationStreamingPolicyNotSupportedException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichPublicationStreamingPolicyNotSupportedException"/> class.
        /// </summary>
        /// <param name="streamingPolicy">The requested streamingPolicy that which is not supported.</param>
        /// <param name="message">The base exception message you want to set.</param>
        public GridwichPublicationStreamingPolicyNotSupportedException(string streamingPolicy, string message)
         : base(message, LogEventIds.PublicationStreamingPolicyNotSupported, null)
        {
            SafeAddToData("streamingPolicy", streamingPolicy);
        }
    }
}
