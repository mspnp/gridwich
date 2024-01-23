using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions
{
    /// <summary>
    /// Exception that is thrown when an invalid content is obtained in a blob http range download.
    /// </summary>
    public class GridwichMediaInfoInvalidContentException : GridwichException
    {
        private const string BlobUriKey = "blobUri";
        private const string DesiredOffsetKey = "desiredOffset";

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaInfoInvalidContentException"/> class.
        /// </summary>
        /// <param name="blobUri">The uri of the blob upon which the operation failed.</param>
        /// <param name="desiredOffset">The offset requested on the download operation.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaInfoInvalidContentException(Uri blobUri, long desiredOffset, string message, JObject operationContext)
            : base(message, LogEventIds.MediaInfoInvalidContent, operationContext)
        {
            SafeAddToData(BlobUriKey, blobUri);
            SafeAddToData(DesiredOffsetKey, desiredOffset);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaInfoInvalidContentException"/> class.
        /// </summary>
        /// <param name="blobUri">The uri of the blob upon which the operation failed.</param>
        /// <param name="desiredOffset">The offset requested on the download operation.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichMediaInfoInvalidContentException(Uri blobUri, long desiredOffset, string message, JObject operationContext, Exception innerException)
            : base(message, LogEventIds.MediaInfoInvalidContent, operationContext, innerException)
        {
            SafeAddToData(BlobUriKey, blobUri);
            SafeAddToData(DesiredOffsetKey, desiredOffset);
        }
    }
}