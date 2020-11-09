output "logic_app_service_principal_id" {
  value = azurerm_template_deployment.logicapp.outputs["logicAppServicePrincipalId"]
}
