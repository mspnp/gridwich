using System;
using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions
{
    /// <summary>
    /// Exception that wraps unexpected MediaInfoLib exceptions.
    /// </summary>
    public class GridwichMediaInfoLibUnexpectedException : GridwichException
    {
        private const string BlobUriKey = "blobUri";

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaInfoLibUnexpectedException"/> class.
        /// </summary>
        /// <param name="blobUri">The uri of the blob upon which the operation failed.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The log event identifier.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaInfoLibUnexpectedException(Uri blobUri, string message, EventId logEventId, JObject operationContext)
            : base(message, logEventId, operationContext)
        {
            SafeAddToData(BlobUriKey, blobUri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaInfoLibUnexpectedException"/> class.
        /// </summary>
        /// <param name="blobUri">The uri of the blob upon which the operation failed.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The log event identifier.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichMediaInfoLibUnexpectedException(Uri blobUri, string message, EventId logEventId, JObject operationContext, Exception innerException)
            : base(message, logEventId, operationContext, innerException)
        {
            SafeAddToData(BlobUriKey, blobUri);
        }
    }
}