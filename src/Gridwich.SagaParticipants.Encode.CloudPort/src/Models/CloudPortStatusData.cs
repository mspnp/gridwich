using System.Collections.Generic;
using Gridwich.Core.Helpers;
using Gridwich.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.CloudPort.Models
{
#pragma warning disable SA1600 // Elements should be documented
    public class CloudPortStatusData : IExternalEventData
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }
        [JsonProperty("workflow_id", Required = Required.Always)]
        public string WorkflowId { get; set; }
        [JsonProperty("status", Required = Required.Always)]
        public string Status { get; set; }
        [JsonProperty("state", Required = Required.Always)]
        public string State { get; set; }
        [JsonProperty("progress", Required = Required.Always)]
        public int Progress { get; set; }
        [JsonConverter(typeof(StringTypeConverter))]
        [JsonProperty("payload", Required = Required.Always)]
        public CloudPortPayload Payload { get; set; }
        [JsonProperty("action_jobs", Required = Required.Always)]
        public IEnumerable<object> ActionJobs { get; set; }

        public JObject GetOperationContext()
        {
            return Payload?.OperationContext;
        }
    }

#pragma warning restore SA1600 // Elements should be documented
}
