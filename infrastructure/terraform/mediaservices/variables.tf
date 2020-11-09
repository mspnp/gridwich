variable "appname" {
  type = string
}

variable "environment" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "scope_ids" {
  type = list(string)
}

variable "subscription_id" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "key_vault_name" {
  type = string
}

variable "ams_v2_callback_endpoint" {
  type = string
}

variable "amsDrm_OpenIdConnectDiscoveryDocument_endpoint" {
  type = string
}

variable "amsDrm_EnableContentKeyPolicyUpdate" {
  type = string
}