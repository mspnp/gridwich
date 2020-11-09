using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Publication.MediaServicesV3.Services;

using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Models
{
    /// <summary>
    /// This type is return by the <see cref="IMediaServicesV3PublicationService"/> when calling LocatorDelete.
    /// </summary>
    public class ServiceOperationResultMediaServicesV3LocatorDelete : ServiceOperationResultBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceOperationResultMediaServicesV3LocatorDelete"/> class.
        /// </summary>
        /// <param name="locatorName">LocatorName which was published.</param>
        /// <param name="operationContext">The OperationContext that triggered this service call.</param>
        public ServiceOperationResultMediaServicesV3LocatorDelete(string locatorName, JObject operationContext)
            : base(operationContext)
        {
            LocatorName = locatorName;
        }

        /// <summary>
        /// Gets locatorName which was published.
        /// </summary>
        public string LocatorName { get; }
    }
}