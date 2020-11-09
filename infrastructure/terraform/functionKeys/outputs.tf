output "host_key" {
  value     = lookup(azurerm_template_deployment.function_keys.outputs, "functionkey")
  sensitive = true
}