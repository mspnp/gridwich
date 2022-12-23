output "functionapp_endpoint_base" {
  value = azurerm_windows_function_app.fxn.default_hostname
}

output "function_app_name" {
  value = azurerm_windows_function_app.fxn.name
}

output "function_app_id" {
  value = azurerm_windows_function_app.fxn.id
}

output "principal_id" {
  value = azurerm_windows_function_app.fxn.identity[0].principal_id
}
