##################################################################################
# Storage accounts module
##################################################################################

locals {
  resource_groups = flatten([for num in range(var.accounts_amount) : [
    var.only_one_resource_group ? format("%s-%s-rg-%s", var.appname, var.resource_group_name, var.environment) : format("%s-%s%02.0f-rg-%s", var.appname, var.resource_group_name, num, var.environment)
  ]])
  storages = flatten([for detail in var.accounts_details : [
    for num in range(var.accounts_amount) : {
      # By using a string as the key for the foreach, we can remove count or groups individually
      simple_account_name : format("%s%02.0f", detail.account_name, num),
      full_account_name : lower(format("%s%s%02.0fsa%s", var.appname, detail.account_name, num, var.environment)),
      containers : detail.containers,
      resource_group_name : local.resource_groups[num],
      cors : detail.cors
    }
  ]])

  # But the trick above means that azurerm_storage_account becomes a map, so we have to extract the info like so
  storages_ids = flatten([for key, storage in azurerm_storage_account.storage_accounts : [
    storage.id
  ]])
  storages_access_keys = flatten([for key, storage in azurerm_storage_account.storage_accounts : [
    {
      name = storage.name,
      key  = storage.primary_access_key
    }
  ]])
  containers = flatten([for storage in local.storages : [
    for container in storage.containers : {
      storage        = storage,
      container_name = container
    }
  ]])
}

##################################################################################
# RESOURCES
##################################################################################

resource "azurerm_resource_group" "resource_groups" {
  count = var.only_one_resource_group ? 1 : var.accounts_amount

  name     = lower(local.resource_groups[count.index])
  location = var.location
  tags     = var.tags
}

resource "azurerm_storage_account" "storage_accounts" {
  depends_on = [azurerm_resource_group.resource_groups]

  for_each = {
    for storage in local.storages : "${storage.simple_account_name},${storage.resource_group_name}" => storage
  }

  name                     = each.value.full_account_name
  resource_group_name      = each.value.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  blob_properties {
    dynamic "cors_rule" {
      for_each = each.value.cors
      content {
        allowed_headers    = cors_rule.value.allowed_headers
        allowed_methods    = cors_rule.value.allowed_methods
        allowed_origins    = cors_rule.value.allowed_origins
        exposed_headers    = cors_rule.value.exposed_headers
        max_age_in_seconds = cors_rule.value.max_age_in_seconds
      }
    }
  }

  location = var.location
  tags     = var.tags
 
  lifecycle {
    ignore_changes = [
      allow_nested_items_to_be_public
    ]
  }
}

resource "azurerm_storage_container" "storage_containers" {
  depends_on = [azurerm_storage_account.storage_accounts]
  for_each = {
    for container in local.containers : "${container.storage.simple_account_name},${container.container_name}" => container
  }

  name                  = each.value.container_name
  storage_account_name  = each.value.storage.full_account_name
  container_access_type = "private"
}
