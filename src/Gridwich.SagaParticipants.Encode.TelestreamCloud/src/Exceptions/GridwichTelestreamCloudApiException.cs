using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.SagaParticipants.Encode.TelestreamCloud.Exceptions
{
    /// <summary>
    /// Throws when an CloudPort is called and there is an event type mismatch.
    /// </summary>
    public class GridwichTelestreamCloudApiException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichTelestreamCloudApiException" /> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">EventId of the log to be thrown.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichTelestreamCloudApiException(string message, EventId logEventId, JObject operationContext)
         : base(message, logEventId, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichTelestreamCloudApiException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">EventId of the log to be thrown.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">An inner exception to wrap.</param>
        public GridwichTelestreamCloudApiException(string message, EventId logEventId, JObject operationContext, Exception innerException)
         : base(message, logEventId, operationContext, innerException)
        {
        }
    }
}
