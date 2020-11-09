using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Exceptions;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode;
using Gridwich.SagaParticipants.Encode.CloudPort.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telestream.Cloud.VantageCloudPort.Client;
using Telestream.Cloud.VantageCloudPort.Model;

namespace Gridwich.SagaParticipants.Encode.CloudPort.Services
{
    /// <summary>
    /// CloudPort implementation of the CloudPort class.
    /// </summary>
    public class CloudPortService : ICloudPortService
    {
        private readonly IStorageService _storageService;
        private readonly ITelestreamCloudClientProvider _telestreamCloudClientProvider;
        private readonly ITelestreamCloudStorageProvider _telestreamCloudStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudPortService"/> class.
        /// </summary>
        /// <param name="storageService">IStorageService storageService.</param>
        /// <param name="telestreamCloudClientProvider">Client provider to Telestream API.</param>
        /// <param name="telestreamCloudStorageProvider">Storage Reference service for Telestream storage.</param>
        public CloudPortService(IStorageService storageService, ITelestreamCloudClientProvider telestreamCloudClientProvider, ITelestreamCloudStorageProvider telestreamCloudStorageProvider)
        {
            _storageService = storageService;
            _telestreamCloudClientProvider = telestreamCloudClientProvider;
            _telestreamCloudStorageProvider = telestreamCloudStorageProvider;
        }

        /// <summary>
        /// Does the heavy lifting, encodes the asset.
        /// </summary>
        /// <param name="cloudPortEncodeCreateDTO">Encode context data object.</param>
        /// <returns>A unique Job Id in string format.</returns>
        public async Task<ServiceOperationResultEncodeDispatched> EncodeCreateAsync(RequestCloudPortEncodeCreateDTO cloudPortEncodeCreateDTO)
        {
            _ = cloudPortEncodeCreateDTO ?? throw new ArgumentNullException(nameof(cloudPortEncodeCreateDTO));

            // EncodeAsync is broken into 2 parts
            //  1. Configure any storage needs for the encoder
            //  2. Call the encode

            // 1. configure storage for encoder
            string sasUri;
            TimeSpan ttl = cloudPortEncodeCreateDTO.SecToLive == 0 ? TimeSpan.FromHours(6) : TimeSpan.FromSeconds(cloudPortEncodeCreateDTO.SecToLive);
            var inputs = cloudPortEncodeCreateDTO.Inputs.ToArray();
            var context = new StorageClientProviderContext(cloudPortEncodeCreateDTO.OperationContext);
            var input = new Uri(inputs[0].BlobUri);

            var exists = await _storageService.GetBlobExistsAsync(input, context).ConfigureAwait(false);
            if (!exists)
            {
                throw new GridwichCloudPortMissingInputException(
                        $"Attempt to use nonexistent blob as input: {input}",
                        input.AbsoluteUri, context.ClientRequestIdAsJObject);
            }
            try
            {
                sasUri = _storageService.GetSasUrlForBlob(input, ttl, context);
            }
            catch (Exception e)
            {
                throw new GridwichCloudPortSASException($"Failed to generate SAS for: {input}", cloudPortEncodeCreateDTO.OperationContext, e);
            }

            if (string.IsNullOrEmpty(sasUri))
            {
                throw new GridwichCloudPortSASException($"Failed to generate SAS for: {input}", cloudPortEncodeCreateDTO.OperationContext);
            }

            // 2. Execute Encode
            var result = await CreateWorkflowJobAsync(sasUri, string.Empty, cloudPortEncodeCreateDTO).ConfigureAwait(false);
            return new ServiceOperationResultEncodeDispatched(
                workflowJobName: result.Id,
                null,
                cloudPortEncodeCreateDTO.OperationContext);
        }

        private async Task<WorkflowJob> CreateWorkflowJobAsync(string inputURL, string jobName, RequestCloudPortEncodeCreateDTO cloudPortEncodeCreateDTO)
        {
            var workflow = await GetWorkflowByNameAsync(cloudPortEncodeCreateDTO.WorkflowName).ConfigureAwait(false);
            var store = await _telestreamCloudStorageProvider.GetStoreByNameAsync(new Uri(cloudPortEncodeCreateDTO.OutputContainer)).ConfigureAwait(false);

            var workflowJob = new WorkflowJob
            {
                Name = jobName
            };

            // We need to pass all source files
            var workflowJobSources = new Dictionary<string, string>();
            foreach (var source in workflow.Input.Sources)
            {
                workflowJobSources[source.Key] = inputURL;
            }

            var variables = ProcessWorkflowVariables(workflow, cloudPortEncodeCreateDTO);

            workflowJob.Inputs = new WorkflowJobInputs(sources: workflowJobSources, variables: variables);

            // Configure the encode payload for OpContext and other data that needs to be tunneled thru the payload.
            // Pass the OutputContainer in the Payload, so we can get it back from CloudPort.
            var payload = new CloudPortPayload()
            {
                OperationContext = cloudPortEncodeCreateDTO.OperationContext,
                OutputContainer = cloudPortEncodeCreateDTO.OutputContainer
            };

            workflowJob.Payload = JsonConvert.SerializeObject(payload);

            // The Storage Reference section of the Input is where the workflow will advertise which Actions (denoted by the Identifier associated with those actions) which have been configured to emit outputs.
            // Additionally, this information will indicate which outputs are TRANSIENT (or temporary) and which outputs are PERMANENT (or intended to exist beyond the lifetime of the job)
            // The intent of the Storage Reference section is to provide the entity which is submitting to this workflow the requisite information necessary to specify the desired STORAGE that the action should utilize when running.
            workflowJob.StorageReferences = new Dictionary<string, WorkflowJobStorageReferences>();

            foreach (var workflowStorageReference in workflow.Input.StorageReferences)
            {
                workflowJob.StorageReferences[workflowStorageReference.Key] = new WorkflowJobStorageReferences(
                    storeId: store.Id,
                    folderOffset: ":id");  // use the job id as a blob name prefix.
            }

            try
            {
                var job = await _telestreamCloudClientProvider.CloudPortApi.CreateWorkflowJobAsync(workflow.Id, workflowJob).ConfigureAwait(false);
                return job;
            }
            catch (ApiException ae)
            {
                throw new GridwichCloudPortApiException("Error calling CreateWorkflowJobAsync.", cloudPortEncodeCreateDTO.OperationContext, ae);
            }
        }

        private async Task<Workflow> GetWorkflowByNameAsync(string workFlowName)
        {
            var workflows = await _telestreamCloudClientProvider.CloudPortApi.ListWorkflowsAsync().ConfigureAwait(false);
            var workflow = workflows.Workflows.FirstOrDefault(w => w.Name == workFlowName);
            return workflow ?? throw new GridwichCloudPortWorkflowDoesNotExistException($"Workflow \"{workFlowName}\" does not exist.", null);
        }

        /// <summary>
        /// Takes a WorkflowId and and WorkflowJobId, usually from an EventGrid status payload, and returns detailed information about that job.
        /// </summary>
        /// <param name="workflowId">The Id of the workflow.</param>
        /// <param name="workflowJobId">The Id of the workflow job.</param>
        /// <returns>A WorkflowJob object.</returns>
        public async Task<WorkflowJob> GetWorkflowJobInfo(string workflowId, string workflowJobId)
        {
            _ = workflowId ?? throw new ArgumentNullException(nameof(workflowId));
            _ = workflowJobId ?? throw new ArgumentNullException(nameof(workflowJobId));

            var workflowJobInfo = await _telestreamCloudClientProvider.CloudPortApi.GetWorkflowJobAsync(workflowId, workflowJobId).ConfigureAwait(false);
            return workflowJobInfo;
        }

        private static Dictionary<string, string> ProcessWorkflowVariables(Workflow workflow, RequestCloudPortEncodeCreateDTO cloudPortEncodeCreateDTO)
        {
            _ = workflow ?? throw new ArgumentNullException(nameof(workflow));
            _ = cloudPortEncodeCreateDTO ?? throw new ArgumentNullException(nameof(cloudPortEncodeCreateDTO));

            var outputVars = new Dictionary<string, string>();

            // Handle the case where workflow takes 0 parameters.
            if (workflow.Input is null || workflow.Input.Variables is null)
            {
                // If supplied parameter count is also 0, we are good.
                if (cloudPortEncodeCreateDTO.Parameters is null || cloudPortEncodeCreateDTO.Parameters.Count == 0)
                {
                    return outputVars;
                }
                else
                {
                    throw new GridwichArgumentException(string.Empty, $"Workflow takes 0 variables, but {cloudPortEncodeCreateDTO.Parameters.Count} parameters were supplied.", LogEventIds.CloudPortParameterError, cloudPortEncodeCreateDTO.OperationContext);
                }
            }

            // Add all params and set to their default values
            foreach (var cloudPortVar in workflow.Input.Variables)
            {
                outputVars.Add(cloudPortVar.Key, cloudPortVar.Value.Default);
            }

            if (cloudPortEncodeCreateDTO.Parameters is null || cloudPortEncodeCreateDTO.Parameters.Count == 0)
            {
                return outputVars;
            }

            // Convert our Json parameter payload to a Dictionary to make it easy to walk thru.
            var parameters =
            cloudPortEncodeCreateDTO.Parameters.Children<JObject>()
                .ToDictionary(
                x => x.Properties().First().Name,
                x => x.Properties().First().Value.ToString());

            foreach (var p in parameters)
            {
                if (workflow.Input.Variables.ContainsKey(p.Key))
                {
                    outputVars[p.Key] = p.Value;
                }
                else
                {
                    var cloudPortParameterException = new GridwichArgumentException(p.Key, $"Invalid parameter: Key: {p.Key}, Value: {p.Value}", LogEventIds.CloudPortParameterError, cloudPortEncodeCreateDTO.OperationContext);
                    foreach (var v in workflow.Input.Variables)
                    {
                        cloudPortParameterException.SafeAddToData(v.Key, v.Value);
                    }
                    throw cloudPortParameterException;
                }
            }

            return outputVars;
        }
    }
}