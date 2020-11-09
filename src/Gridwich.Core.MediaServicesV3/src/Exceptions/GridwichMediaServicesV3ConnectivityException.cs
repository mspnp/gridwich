using System;

using Gridwich.Core.Exceptions;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichMediaServicesV3ConnectivityException"/>.
    /// </summary>
    public class GridwichMediaServicesV3ConnectivityException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3ConnectivityException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaServicesV3ConnectivityException(string message, EventId logEventId)
         : base(message, logEventId, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3ConnectivityException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichMediaServicesV3ConnectivityException(string message, EventId logEventId, Exception innerException)
         : base(message, logEventId, null, innerException)
        {
        }
    }
}
