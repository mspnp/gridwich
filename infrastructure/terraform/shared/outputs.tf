output "shared_key_vault_id" {
  value = azurerm_key_vault.shared_key_vault.id
}

output "delivery_topic_endpoint" {
  value = azurerm_eventgrid_topic.grw_topic.endpoint
}

output "delivery_topic_id" {
  value = azurerm_eventgrid_topic.grw_topic.id
}

output "delivery_topic_key" {
  value = azurerm_eventgrid_topic.grw_topic.primary_access_key
}

output "shared_kv_name" {
  value = azurerm_key_vault.shared_key_vault.name
}

output "log_analytics_workspace_id" {
  value = azurerm_log_analytics_workspace.loganalytics.id
}

output "dead_letter_sa_id" {
  value = azurerm_storage_account.topic_dead_letter_sa.id
}

output "dead_letter_container_name" {
  value = azurerm_storage_container.topic_dead_letter_container.name
}