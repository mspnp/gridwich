variable "appname" {
  type        = string
  description = "Application name. Use only lowercase letters and numbers"
  default     = "gw"
}

variable "domainprefix" {
  type        = string
  description = "Domain prefix. Use only lowercase letters and numbers"
  default     = "grw"
}

variable "environment" {
  type        = string
  description = "Environment name, e.g. 'dev' or 'stage'"
  default     = "dev"
}

variable "location" {
  type        = string
  description = "Azure region where to create resources."
  default     = "West US"
}

variable "department" {
  type        = string
  description = "Passed from the build pipeline and used to tag resources."
  default     = "gridwichTeam"
}

variable "extsys_event_endpoint" {
  type        = string
  description = "The ExtSys endpoint to send events, includes http scheme and full path"
}

variable "telestream_cloud_api_key" {
  type        = string
  description = "The REST API key needed to access the Flip encoder service."
}

variable "event_grid_function_name" {
  type        = string
  description = "The name of the Function which handles Event Grid messages"
}

variable "pipeline_build_id" {
  type        = string
  description = "The ID of the build in the CICD Pipeline. This is used to ensure certain resource deployments run on every build"
}

variable "sp_client_secret" {
  type        = string
  description = "The client secret of the service principal running the deployment; used by the Secret Change Handler to set up Event Grid connectivity"
}

variable "run_flag_subscriptions_fail_gracefully" {
  type        = bool
  default     = false
  description = "Flag to signal how subscriptions should behave. When true, subscriptions will fail gracefully."
}

variable "run_flag_subscriptions_disabled" {
  type        = bool
  default     = false
  description = "Flag to signal how subscriptions should behave. When true, subscriptions won't be created."
}

variable "entraid_domain" {
  type        = string
  description = "The Microsoft Entra ID Primary Domain.  Used by the EventGrid Viewer Blazor application to enable Microsoft Entra ID authentication."
  default     = "microsoft.onmicrosoft.com"
}