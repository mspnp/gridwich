variable "appname" {
  type = string
}


variable "environment" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "accounts_details" {
  type = list(object({
    account_name = string
    containers   = list(string)
    cors = list(object({
      allowed_headers    = list(string)
      allowed_methods    = list(string)
      allowed_origins    = list(string)
      exposed_headers    = list(string)
      max_age_in_seconds = number
    }))
  }))
}

variable "accounts_amount" {
  type = number
}

variable "only_one_resource_group" {
  type        = bool
  description = "If true, all storage accounts will live in the same resource group. If false, each iteration of accounts_amount will live in a new resource group"
}

variable "location" {
  type = string
}

variable "tags" {
  type = map(string)
}

variable "cors" {
  type = list(object({
    allowed_headers    = list(string)
    allowed_methods    = list(string)
    allowed_origins    = list(string)
    exposed_headers    = list(string)
    max_age_in_seconds = number
  }))
  default = []
}
