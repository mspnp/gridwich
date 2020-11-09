using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Occurs when an encode job fails.
    /// </summary>
    public class GridwichEncodeJobFailedException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichEncodeJobFailedException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichEncodeJobFailedException(string message, JObject operationContext)
         : base(message, LogEventIds.EncodeCompleteFailure, operationContext)
        {
        }
    }
}
