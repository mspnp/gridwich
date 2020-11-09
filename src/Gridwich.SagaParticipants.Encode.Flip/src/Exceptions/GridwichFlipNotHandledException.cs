using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichFlipNotHandledException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipNotHandledException"/> class.
        /// </summary>
        /// <param name="locatorName">The requested locatorName which was not found.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichFlipNotHandledException(string message, JObject operationContext)
         : base(message, LogEventIds.FlipDoesNotHandleThisType, operationContext)
        {
        }
    }
}
