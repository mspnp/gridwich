# The Gridwich Project 🎆🥪

A framework for stateless workloads (batteries included for video on-demand operations). For more background, see [Gridwich architecture](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture).

## Features

* Stateless workloads allow an arbituary object to flow through each workload. See [Operation context](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#operation-context).
* Easily customize by hooking [custom Event Grid listeners](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#gridwich-sandwiches).
* Object logging is integrated with Azure Application Insights. See [Gridwich logging](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-logging).
* Deployment uses Azure Pipelines and Terraform.

## Getting started

* [Set up the Azure DevOps Project and Azure Pipelines](doc/1-set-up-azure-devops.md).

## Concepts

* [Gridwich architecture](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture).
* [Clean monolith design](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-clean-monolith).
* [Saga orchestration](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/saga-orchestration).
* [Request and response flow](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#request-flow).
* [Operation context](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#operation-context).
* [Synchronous and asynchronous handlers](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#sync-and-async-handlers).
* [CI/CD patterns](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-cicd).
* [Azure Pipelines to Terraform variable flow](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/variable-group-terraform-flow).
* [Gridwich Storage Provider](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-storage-service).
* [Gridwich ObjectLogger](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-logging#objectlogger).

## Procedures

1. [Set up Azure Project and Azure Pipelines](doc/1-set-up-azure-devops.md).
1. [Run pipeline-generated admin scripts](doc/2-run-admin-scripts.md).
1. [Maintain and rotate keys and secrets](doc/3-maintain-keys.md).
1. [Set up a local development environment](doc/4-set-up-local-environment.md).
1. [Create or delete a cloud environment](doc/5-create-delete-cloud-environment.md).
1. [Test Mediainfo service](doc/6-test-mediainfo.md).

## Resources

* [Azure deployment diagram](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/create-delete-cloud-environment#azure-resources).
* [Project naming conventions](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-project-names).
* [Gridwich message formats](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-message-formats).
* [Long running function deployments](https://docs.microsoft.com/azure/architecture/reference-architectures/media-services/gridwich-architecture#long-running-functions).
* [Telestream workflow definitions (JSON source file)](Resources_Telestream_Workflow_Definitions.json)
