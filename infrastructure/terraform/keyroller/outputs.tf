output "logic_app_service_principal_id" {
  value = azurerm_template_deployment.keyroller_logicapp.outputs["logicAppServicePrincipalId"]
}
