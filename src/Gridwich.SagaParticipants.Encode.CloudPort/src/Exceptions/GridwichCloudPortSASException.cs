using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichCloudPortSASException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortSASException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichCloudPortSASException(string message, JObject operationContext)
         : base(message, LogEventIds.CloudPortSASError, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortSASException" /> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The inner exception.</param>
        public GridwichCloudPortSASException(string message, JObject operationContext, Exception innerException)
         : base(message, LogEventIds.CloudPortSASError, operationContext, innerException)
        {
        }
    }
}