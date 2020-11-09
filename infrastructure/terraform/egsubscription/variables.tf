variable "endpoint" {}
variable "scope_ids" {
  type = list(string)
}

variable "fail_gracefully" {
  type    = bool
  default = false
}

variable "subscriptions_disabled" {
  type    = bool
  default = false
}

variable "name" {}

variable "dead_letter_sa_id" {
  type    = string
  default = null
}
variable "dead_letter_container_name" {
  type    = string
  default = null
}