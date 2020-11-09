using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when an specified workflow does not exist.
    /// </summary>
    public class GridwichCloudPortWorkflowDoesNotExistException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichCloudPortWorkflowDoesNotExistException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichCloudPortWorkflowDoesNotExistException(string message, JObject operationContext)
         : base(message, LogEventIds.CloudPortWorkflowDoesNotExist, operationContext)
        {
        }
    }
}
