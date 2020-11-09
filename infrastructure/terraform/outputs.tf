output "subscription_id" {
  value = data.azurerm_client_config.current.subscription_id
}

output "delivery_topic_endpoint" {
  value = module.shared.delivery_topic_endpoint
}

output "delivery_topic_id" {
  value = module.shared.delivery_topic_id
}

output "function_app_name" {
  value = module.functions.function_app_name
}

output "function_app_resource_group" {
  value = azurerm_resource_group.application.name
}

output "shared_kv_name" {
  value = module.shared.shared_kv_name
}

output "event_viewer_base" {
  value = module.event_viewer.event_viewer_base_endpoint
}

output "azurerm_media_services_account_resource_id" {
  value = module.mediaservices.azurerm_media_services_account_resource_id
}