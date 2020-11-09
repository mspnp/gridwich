using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an exception when listing files.
    /// </summary>
    public class GridwichCloudPortOutputListingException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortOutputListingException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichCloudPortOutputListingException(string message, JObject operationContext)
         : base(message, LogEventIds.EncodeOutputFailure, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortOutputListingException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichCloudPortOutputListingException(string message, JObject operationContext, Exception innerException)
         : base(message, LogEventIds.EncodeOutputFailure, operationContext, innerException)
        {
        }
    }
}
