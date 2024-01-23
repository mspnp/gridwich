using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichEncodeCreateJobException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichEncodeCreateJobException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">A specific LogEventId.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The exception that spawned this one.</param>
        public GridwichEncodeCreateJobException(string message, JObject operationContext, Exception innerException, EventId logEventId)
         : base(message, logEventId, operationContext, innerException)
        {
        }
    }
}
