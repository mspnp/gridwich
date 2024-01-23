using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.Core.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichArgumentException"/>.
    /// </summary>
    public class GridwichArgumentException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichArgumentException"/> class.
        /// </summary>
        /// <param name="paramName">The param which was not expected.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichArgumentException(string paramName, string message, EventId logEventId, JObject operationContext)
         : base(message, logEventId, operationContext)
        {
            Data.Add("paramName", paramName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichArgumentException"/> class.
        /// </summary>
        /// <param name="paramName">The param which was not expected.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichArgumentException(string paramName, string message, EventId logEventId, JObject operationContext, Exception innerException)
         : base(message, logEventId, operationContext, innerException)
        {
            Data.Add("paramName", paramName);
        }
    }
}
