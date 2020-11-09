output "storage_account_ids" {
  value = local.storages_ids
}

output "resource_group_names" {
  value = azurerm_resource_group.resource_groups[*].name
}

output "resource_group_ids" {
  value = azurerm_resource_group.resource_groups[*].id
}
