using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Thrown when Media Services is called and there is an event type mismatch.
    /// </summary>
    public class GridwichMediaServicesV3NotHandledException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3NotHandledException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaServicesV3NotHandledException(string message, JObject operationContext)
         : base(message, LogEventIds.MediaServicesV3InvalidEventTypeError, operationContext)
        {
        }
    }
}
