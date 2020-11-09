using System;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Exceptions
{
    /// <summary>
    /// Exception thrown when errors are found in MediaServicesV2.
    /// </summary>
    public class GridwichMediaServicesV2MessageProcessingException : GridwichException
    {
        private const string MessageKey = "message";

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV2MessageProcessingException"/> class.
        /// </summary>
        /// <param name="notificationMessage">The message being processed.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaServicesV2MessageProcessingException(MediaServicesV2NotificationMessage notificationMessage, string message, EventId logEventId, JObject operationContext)
            : base(message, logEventId, operationContext)
        {
            SafeAddToData(MessageKey, notificationMessage);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV2MessageProcessingException" /> class.
        /// </summary>
        /// <param name="notificationMessage">The notification message.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The cause behind this exception.</param>
        public GridwichMediaServicesV2MessageProcessingException(MediaServicesV2NotificationMessage notificationMessage, string message, EventId logEventId, JObject operationContext, Exception innerException)
            : base(message, logEventId, operationContext, innerException)
        {
            SafeAddToData(MessageKey, notificationMessage);
        }
    }
}