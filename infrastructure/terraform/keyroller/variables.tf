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

variable "event_grid_topic_id" {
  type = string
}

variable "event_grid_topic_endpoint" {
  type = string
}

variable "event_grid_topic_key" {
  type = string
}

variable "cache_busting" {
  type = string
}

variable "log_analytics_workspace_id" {
  type = string
}
