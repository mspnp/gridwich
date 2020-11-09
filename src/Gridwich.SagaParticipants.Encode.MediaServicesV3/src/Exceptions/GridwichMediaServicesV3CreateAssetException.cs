using System;
using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichMediaServicesV3CreateAssetException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3CreateAssetException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichMediaServicesV3CreateAssetException(string message, EventId logEventId, JObject operationContext, Exception innerException)
         : base(message, logEventId, operationContext, innerException)
        {
        }
    }
}
