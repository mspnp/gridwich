variable "appname" {
  type = string
}

variable "key_vault_tenant_id" {
  type = string
}

variable "access_policy_object_ids" {
  type = list(string)
}

variable "functions_principal_id" {
  type = string
}

variable "environment" {
  type = string
}

variable "domainprefix" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "telestream_cloud_api_key" {
  type = string
}

variable "amsDrm_FairPlay_Pfx_Password" {
  type        = string
  description = "The password of the FairPlay Pfx password"
}

variable "amsDrm_FairPlay_Ask_Hex" {
  type        = string
  description = "The FairPlay Ask key in Hex format"
}
