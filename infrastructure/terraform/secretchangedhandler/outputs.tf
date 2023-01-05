output "logic_app_service_principal_id" {
  value = azurerm_resource_group_template_deployment.logicapp.output_content == "{}" ? "": jsondecode(azurerm_resource_group_template_deployment.logicapp.output_content).logicAppServicePrincipalId.value
}
