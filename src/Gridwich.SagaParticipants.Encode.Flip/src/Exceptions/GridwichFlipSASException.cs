using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichFlipSASException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipSASException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichFlipSASException(string message, JObject operationContext)
         : base(message, LogEventIds.FlipSASError, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipSASException" /> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public GridwichFlipSASException(string message, JObject operationContext, Exception innerException)
         : base(message, LogEventIds.FlipSASError, operationContext, innerException)
        {
        }
    }
}