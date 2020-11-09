using Gridwich.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gridwich.Core.MediaServicesV3.Exceptions
{
    /// <summary>
    /// Exception thrown when errors are found in MediaServicesV3.
    /// </summary>
    public class GridwichMediaServicesV3Exception : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichMediaServicesV3Exception"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="logEventId">The last LogEventId that was logged relevant to this exception.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichMediaServicesV3Exception(string message, EventId logEventId, JObject operationContext)
            : base(message, logEventId, operationContext)
        {
        }
    }
}