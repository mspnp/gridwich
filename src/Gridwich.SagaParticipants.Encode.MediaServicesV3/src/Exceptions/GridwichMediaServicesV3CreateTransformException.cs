using System;
using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichMediaServicesV3CreateTransformException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3CreateTransformException"/> class.
        /// </summary>
        /// <param name="locatorName">The requested locatorName which was not found.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichMediaServicesV3CreateTransformException(string message, EventId logEventId, JObject operationContext, Exception innerException)
         : base(message, logEventId, operationContext, innerException)
        {
        }
    }
}
