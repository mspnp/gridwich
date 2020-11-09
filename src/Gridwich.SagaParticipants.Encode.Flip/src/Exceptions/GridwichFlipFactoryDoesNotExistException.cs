using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Exceptions
{
    /// <summary>
    /// Throws when specified Flip Factory does not exist.
    /// </summary>
    public class GridwichFlipFactoryDoesNotExistException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichFlipFactoryDoesNotExistException"/> class.
        /// </summary>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="operationContext">The OperationContext for this exception.</param>
        public GridwichFlipFactoryDoesNotExistException(string message, JObject operationContext)
         : base(message, LogEventIds.FlipFactoryNotFound, operationContext)
        {
        }
    }
}
