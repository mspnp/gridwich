using Gridwich.Core.Models;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode
{
    /// <summary>
    /// The result of responding to a Encode.[].Create request.
    /// </summary>
    public class ServiceOperationResultEncodeDispatched : ServiceOperationResultBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceOperationResultEncodeDispatched"/> class.
        /// The result of responding to a Encode.[].Create request.
        /// </summary>
        /// <param name="workflowJobName">workflowJobName.</param>
        /// <param name="encoderContext">encoderContext.</param>
        /// <param name="operationContext">operationContext.</param>
        public ServiceOperationResultEncodeDispatched(string workflowJobName, JObject encoderContext, JObject operationContext)
            : base(operationContext)
        {
            WorkflowJobName = workflowJobName;
            EncoderContext = encoderContext;
        }

        /// <summary>
        /// Gets the workflow job name.
        /// </summary>
        public string WorkflowJobName { get; }

        /// <summary>
        /// Gets information that allows the encode to be tracked when debugging.
        /// </summary>
        public JObject EncoderContext { get; }
    }
}
