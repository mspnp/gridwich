using System;
using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.Exceptions
{
    /// <summary>
    /// The loading of the MediaInfo library can fail. This exception will be
    /// throw when we cannot load the library correctly.
    /// </summary>
    public class GridwichMediaInfoLibException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaInfoLibException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaInfoLibException(string message, EventId logEventId, JObject operationContext)
            : base(message, logEventId, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaInfoLibException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichMediaInfoLibException(string message, EventId logEventId, JObject operationContext, Exception innerException)
            : base(message, logEventId, operationContext, innerException)
        {
        }
    }
}