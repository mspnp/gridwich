using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Thrown when the Media Services job is cancelled.
    /// </summary>
    public class GridwichMediaServicesV3JobCancelledException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3JobCancelledException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaServicesV3JobCancelledException(string message, JObject operationContext)
         : base(message, LogEventIds.MediaServicesV3JobCanceledReceived, operationContext)
        {
        }
    }
}
