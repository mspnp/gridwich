using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichFlipApiException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipApiException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichFlipApiException(string message, JObject operationContext)
         : base(message, LogEventIds.CloudPortApiError, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipApiException" /> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public GridwichFlipApiException(string message, JObject operationContext, Exception innerException)
         : base(message, LogEventIds.CloudPortApiError, operationContext, innerException)
        {
        }
    }
}
