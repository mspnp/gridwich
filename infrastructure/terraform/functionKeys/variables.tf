variable "function_app_name" {
  type        = string
  description = "The name of the FunctionApp in to which the Functions were deployed"
}

variable "resource_group_name" {
  type        = string
  description = "The name of the Resource Group the Function App was deployed in to"
}

variable "cache_busting" {
  type        = string
  description = "The host key can change at anytime, and there is no way for TF to know that it changed. This is the value that, which changed, will force TF to run the deployment again"
}