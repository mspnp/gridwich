using Gridwich.Core.DTO;
using System.Threading.Tasks;
using Telestream.Cloud.VantageCloudPort.Model;

namespace Gridwich.SagaParticipants.Encode.CloudPort.Services
{
    /// <summary>
    /// Defines an interface for the CloudPort encoding service.
    /// </summary>
    public interface ICloudPortService
    {
        /// <summary>
        /// Executes an encode in CloudPort.
        /// </summary>
        /// <param name="cloudPortEncodeCreateDTO">An encode create payload for CloudPort.</param>
        /// <returns>Job related informatin in ServiceOperationResultEncodeDispatched.</returns>
        public Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestCloudPortEncodeCreateDTO cloudPortEncodeCreateDTO);

        /// <summary>
        /// Gets workflow related information when a "WorkflowJob" has completed.
        /// </summary>
        /// <param name="workflowId">The Id of the Workflow.</param>
        /// <param name="workflowJobId">The Id of the WorkflowJob.</param>
        /// <returns>A WorkflowJob object.</returns>
        public Task<WorkflowJob> GetWorkflowJobInfo(string workflowId, string workflowJobId);
    }
}