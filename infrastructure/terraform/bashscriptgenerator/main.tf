variable "storage_account_ids" {
  type = list(string)
}

variable "storage_resource_group_ids" {
  type = list(string)
}

variable "logic_app_sch_service_principal_id" {
  default = ""
}

variable "logic_app_kr_service_principal_id" {
  default = ""
}

variable "function_app_id" {
  default = ""
}

variable "function_principal_id" {
}

variable "media_services_name" {
  default = ""
}

variable "media_services_resource_group_name" {
  default = ""
}

variable "media_services_account_resource_id" {
  default = ""
}

variable "key_vault_name" {
  default = ""
}

variable "tenant_id" {
  default = ""
}

variable "event_grid_viewer_app_name" {
  default = ""
}

variable "event_grid_viewer_resource_group_name" {
  default = ""
}

variable "pipeline_build_id" {
  default = ""
}

resource "template_dir" "config" {
  source_dir      = "./bashscriptgenerator/templates"
  destination_dir = "./bash_scripts"

  vars = {
    logicAppSCHServicePrincipalId    = var.logic_app_sch_service_principal_id
    logicAppKRServicePrincipalId     = var.logic_app_kr_service_principal_id
    functionAppId                    = var.function_app_id
    mediaServicesName                = var.media_services_name
    mediaServicesResourceGroupName   = var.media_services_resource_group_name
    mediaServicesAccountResourceId   = var.media_services_account_resource_id
    keyVaultName                     = var.key_vault_name
    storageAccountIds                = join(" ", var.storage_account_ids)
    storageRgIds                     = join(" ", var.storage_resource_group_ids)
    functionPrincipalId              = var.function_principal_id
    tenantId                         = var.tenant_id
    eventgridViewerAppName           = var.event_grid_viewer_app_name
    eventgridViewerResourceGroupName = var.event_grid_viewer_resource_group_name
    pipelineBuildId                  = var.pipeline_build_id
  }
}
