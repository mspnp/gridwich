output "host_key" {
  value     = jsondecode(azurerm_resource_group_template_deployment.function_keys.output_content).functionkey.value
  sensitive = true
}