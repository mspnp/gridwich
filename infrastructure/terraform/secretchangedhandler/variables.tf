variable "subscription_id" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "appname" {
  type = string
}

variable "domainprefix" {
  type = string
}

variable "environment" {
  type = string
}

variable "sp_client_id" {
  type = string
}

variable "sp_client_secret" {
  description = "The Client Secret for the service principal executing this terraform deployment"
  type        = string
}

variable "shared_key_vault_id" {
  type = string
}

variable "function_app_id" {
  type = string
}

variable "secret_names_to_watch" {
  type = list(string)
}

variable "log_analytics_workspace_id" {
  type = string
}
