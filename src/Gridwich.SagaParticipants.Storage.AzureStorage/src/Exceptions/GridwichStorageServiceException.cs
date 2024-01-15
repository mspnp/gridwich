using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.SagaParticipants.Storage.AzureStorage.Exceptions
{
    /// <summary>
    /// Exception that is thrown when a critical condition is met in the Storage Service.
    /// </summary>
    public class GridwichStorageServiceException : GridwichException
    {
        private const string ElementUriDataKey = "elementUri";

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichStorageServiceException"/> class.
        /// </summary>
        /// <param name="elementUri">The uri of the element upon which the operation failed to execute.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichStorageServiceException(Uri elementUri, string message, EventId logEventId, JObject operationContext)
            : base(message, logEventId, operationContext)
        {
            SafeAddToData(ElementUriDataKey, elementUri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichStorageServiceException"/> class.
        /// </summary>
        /// <param name="elementUri">The uri of the element upon which the operation failed to execute.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The cause behind this exception.</param>
        public GridwichStorageServiceException(Uri elementUri, string message, EventId logEventId, JObject operationContext, Exception innerException)
            : base(message, logEventId, operationContext, innerException)
        {
            SafeAddToData(ElementUriDataKey, elementUri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichStorageServiceException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The cause behind this exception.</param>
        public GridwichStorageServiceException(string message, EventId logEventId, JObject operationContext, Exception innerException)
            : base(message, logEventId, operationContext, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichStorageServiceException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichStorageServiceException(string message, EventId logEventId, JObject operationContext)
            : base(message, logEventId, operationContext)
        {
        }
    }
}