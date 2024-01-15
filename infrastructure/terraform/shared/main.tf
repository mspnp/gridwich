##################################################################################
# Shared resources Terraform file
##################################################################################

##################################################################################
# RESOURCES
##################################################################################

resource "random_id" "server" {
  byte_length = 2
}

locals {
  unique_id = "${random_id.server.hex}"
}

resource "azurerm_key_vault" "shared_key_vault" {
  name                = "${var.appname}-kv-${var.environment}-${local.unique_id}"
  location            = var.location
  resource_group_name = var.resource_group_name
  tenant_id           = var.key_vault_tenant_id
  sku_name = "standard"
  soft_delete_retention_days = 7

  dynamic "access_policy" {
    for_each = var.access_policy_object_ids
    content {
      tenant_id = var.key_vault_tenant_id
      object_id = access_policy.value

      key_permissions = [
        "Get",
        "List",
        "Create",
        "Delete",
        "Purge",
        "Recover"
      ]

      secret_permissions = [
        "Get",
        "List",
        "Set",
        "Delete",
        "Purge",
        "Recover"
      ]

      certificate_permissions = [
        "Get",
        "List",
        "Import",
        "Delete",
        "Purge",
        "Recover"
      ]
    }
  }

  access_policy {
    object_id = var.functions_principal_id
    tenant_id = var.key_vault_tenant_id

    secret_permissions = [
      "Get",
    ]

  }

  lifecycle {
    ignore_changes = [
      access_policy
    ]
  }
}

#############################
# Main Topic
#############################

resource "azurerm_eventgrid_topic" "grw_topic" {
  name                = "${var.appname}-grw-egt-${var.environment}-${local.unique_id}"
  location            = var.location
  resource_group_name = var.resource_group_name
}

# This storage account + container are used to store all the events
# that fail to be sent correctly, aka dead letter queue
resource "azurerm_storage_account" "topic_dead_letter_sa" {
  name                     = format("%sdeadlettersa%s", var.appname, var.environment)
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  
  lifecycle {
    ignore_changes = [
      allow_nested_items_to_be_public
    ]
  }
}

resource "azurerm_storage_container" "topic_dead_letter_container" {
  name                  = "egt-dead-letter"
  storage_account_name  = azurerm_storage_account.topic_dead_letter_sa.name
  container_access_type = "private"
}

#############################
# Secrets
#############################

resource "azurerm_key_vault_secret" "grw_topic_key_secret" {
  name         = "grw-topic-key"
  value        = azurerm_eventgrid_topic.grw_topic.primary_access_key
  key_vault_id = azurerm_key_vault.shared_key_vault.id
}

resource "azurerm_key_vault_secret" "grw_topic_end_point_secret" {
  name         = "grw-topic-end-point"
  value        = azurerm_eventgrid_topic.grw_topic.endpoint
  key_vault_id = azurerm_key_vault.shared_key_vault.id
}

resource "azurerm_key_vault_secret" "telestream_cloud_api_key_secret" {
  name         = "telestream-cloud-api-key"
  value        = var.telestream_cloud_api_key
  key_vault_id = azurerm_key_vault.shared_key_vault.id

  lifecycle {
    ignore_changes = [
      value,
      tags
    ]
  }
}

# These are the values watched by the Secret Changed Handler; keep these up to date with what is put in KeyVault above
# and elsewhere or if one of the values for those secrets changes, the Function App using them won't be updated to
# utilize the new value
output "secrets_in_shared_keyvault" {
  value = ["telestream-cloud-api-key", "grw-topic-end-point", "grw-topic-key", "appinsights-connectionstring"]
}

###########################################################
# Log Analytics Workspace (for Logic Apps telemetry)
###########################################################

resource "azurerm_log_analytics_workspace" "loganalytics" {
  name                = format("%s-%s-law-%s-%s", var.appname, var.domainprefix, var.environment, local.unique_id)
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_monitor_diagnostic_setting" "example" {
  name                       = format("%s-%s-mds-%s-%s", var.appname, var.domainprefix, var.environment, local.unique_id)
  target_resource_id         = azurerm_eventgrid_topic.grw_topic.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.loganalytics.id

  log {
    category = "DeliveryFailures"
    enabled  = true

    retention_policy {
      enabled = true
     }
  }

  log {
    category = "PublishFailures"
    enabled  = true

    retention_policy {
      enabled = true
    }
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      enabled = true
    }
  }

  lifecycle {
    ignore_changes = [
      log
    ]
  }
}

##################################################################################
# Functions KeyVault References Terraform file
##################################################################################

locals {
  functions_appsetting_keyvault_refs = [
    {
      name        = "GRW_TOPIC_KEY"
      value       = format("@Microsoft.KeyVault(SecretUri=%ssecrets/%s/)", azurerm_key_vault.shared_key_vault.vault_uri, azurerm_key_vault_secret.grw_topic_key_secret.name)
      slotSetting = false
    },
    {
      name        = "GRW_TOPIC_END_POINT"
      value       = format("@Microsoft.KeyVault(SecretUri=%ssecrets/%s/)", azurerm_key_vault.shared_key_vault.vault_uri, azurerm_key_vault_secret.grw_topic_end_point_secret.name)
      slotSetting = false
    },
    {
      name        = "TELESTREAMCLOUD_API_KEY"
      value       = format("@Microsoft.KeyVault(SecretUri=%ssecrets/%s/)", azurerm_key_vault.shared_key_vault.vault_uri, azurerm_key_vault_secret.telestream_cloud_api_key_secret.name)
      slotSetting = false
    },
    {
      name        = "KeyVaultBaseUrl"
      value       = azurerm_key_vault.shared_key_vault.vault_uri
      slotSetting = false
    }
  ]
}

resource "local_sensitive_file" "app_settings_keyvault_refs_json" {
  content   = jsonencode(local.functions_appsetting_keyvault_refs)
  filename  = "./app_settings/shared_keyvault_refs.json"

  lifecycle {
    ignore_changes = all
  }
}
