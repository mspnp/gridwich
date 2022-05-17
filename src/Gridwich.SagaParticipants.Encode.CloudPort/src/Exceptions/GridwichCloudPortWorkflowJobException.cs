using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and the response is a encode failure event.
    /// </summary>
    public class GridwichCloudPortWorkflowJobException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortWorkflowJobException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichCloudPortWorkflowJobException(string message, JObject operationContext)
         : base(message, LogEventIds.EncodeCompleteFailure, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortWorkflowJobException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public GridwichCloudPortWorkflowJobException(string message, JObject operationContext, Exception innerException)
         : base(message, LogEventIds.EncodeCompleteFailure, operationContext, innerException)
        {
        }
    }
}
