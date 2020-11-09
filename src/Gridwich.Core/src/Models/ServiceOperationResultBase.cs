using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Models
{
    /// <summary>
    /// Use as a base class for specific service operation results.
    /// Those specific service operation result classes should be used
    /// to return complex objects from internal services.
    /// </summary>
    public abstract class ServiceOperationResultBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceOperationResultBase"/> class.
        /// </summary>
        /// <param name="operationContext">The OperationContext that triggered this service call.</param>
        protected ServiceOperationResultBase(JObject operationContext)
        {
            OperationContext = operationContext;
        }

        /// <summary>
        /// Gets the OperationContext.
        /// </summary>
        public JObject OperationContext { get; }
    }
}