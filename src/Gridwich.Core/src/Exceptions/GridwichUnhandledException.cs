using Gridwich.Core.Constants;
using Newtonsoft.Json.Linq;
using System;

namespace Gridwich.Core.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichArgumentException"/>.
    /// </summary>
    public class GridwichUnhandledException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichUnhandledException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The inner exception to include in the Gridwich exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichUnhandledException(string message, Exception innerException, JObject operationContext)
         : base(message, LogEventIds.GridwichUnhandledException, operationContext, innerException)
        {
        }
    }
}