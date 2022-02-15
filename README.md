# The Gridwich Project 🎆🥪

A framework for stateless workloads (batteries included for video on-demand operations). For more background, see [Gridwich architecture](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture).

## Features

* Stateless workloads allow an arbituary object to flow through each workload. See [Operation context](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#operation-context).
* Easily customize by hooking [custom Event Grid listeners](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#gridwich-sandwiches).
* Object logging is integrated with Azure Application Insights. See [Gridwich logging](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-logging).
* Deployment uses Azure Pipelines and Terraform.

## Getting started

* [Set up the Azure DevOps Project and Azure Pipelines](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/set-up-azure-devops).
* [Set up content protection policies and DRM](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-content-protection-drm).
* To add functionality to Gridwich, [set up a local dev environment](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/set-up-local-environment).
* To smoke-test Gridwich, [test Azure Media Services V3 encoding](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/test-encoding).

## Concepts

* [Gridwich architecture](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture).
* [Clean monolith design](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-clean-monolith).
* [Saga orchestration](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/saga-orchestration).
* [Request and response flow](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#request-flow).
* [Operation context](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#operation-context).
* [Synchronous and asynchronous handlers](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#sync-and-async-handlers).
* [CI/CD patterns](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-cicd).
* [Azure Pipelines to Terraform variable flow](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/variable-group-terraform-flow).
* [Content protection and DRM](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-content-protection-drm).
* [Azure Media Services](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/media-services-setup-scale).
* [Gridwich Storage Provider](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-storage-service).
* [Gridwich ObjectLogger](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-logging#objectlogger).

## Procedures

* [Set up Azure Project and Azure Pipelines](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/set-up-azure-devops).
* [Run pipeline-generated admin scripts](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/run-admin-scripts).
* [Maintain and rotate keys and secrets](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/maintain-keys).
* [Set up a local development environment](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/set-up-local-environment).
* [Create or delete a cloud environment](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/create-delete-cloud-environment).
* [Set up content protection and DRM](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-content-protection-drm).
* [Set up and scale Azure Media Services](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/media-services-setup-scale).
* [Test Azure Media Services V3 Encoding](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/test-encoding).

## Resources

* [Azure deployment diagram](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/create-delete-cloud-environment#azure-resources).
* [Project naming conventions](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-project-names).
* [Gridwich message formats](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-message-formats).
* [Long running function deployments](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#long-running-functions).
* [Telestream workflow definitions (JSON source file)](Resources_Telestream_Workflow_Definitions.json)
