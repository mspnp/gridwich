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
        "get",
        "list",
        "create",
        "delete",
        "purge",
        "recover"
      ]

      secret_permissions = [
        "get",
        "list",
        "set",
        "delete",
        "purge",
        "recover"
      ]

      certificate_permissions = [
        "get",
        "list",
        "import",
        "delete",
        "purge",
        "recover"
      ]
    }
  }

  access_policy {
    object_id = var.functions_principal_id
    tenant_id = var.key_vault_tenant_id

    secret_permissions = [
      "get",
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

resource "azurerm_key_vault_secret" "ams_sp_client_id" {
  name         = "ams-sp-client-id"
  value        = "placeholder"
  key_vault_id = azurerm_key_vault.shared_key_vault.id
  lifecycle {
    ignore_changes = [
      value,
      tags
    ]
  }
}

resource "azurerm_key_vault_secret" "ams_sp_client_secret" {
  name         = "ams-sp-client-secret"
  value        = "placeholder"
  key_vault_id = azurerm_key_vault.shared_key_vault.id
  lifecycle {
    ignore_changes = [
      value,
      tags
    ]
  }
}

resource "azurerm_key_vault_secret" "ams_fairplay_pfx_password" {
  name         = "ams-fairplay-pfx-password"
  value        = var.amsDrm_FairPlay_Pfx_Password
  key_vault_id = azurerm_key_vault.shared_key_vault.id

  lifecycle {
    ignore_changes = [
      value,
      tags
    ]
  }
}

resource "azurerm_key_vault_secret" "ams_fairplay_ask_hex" {
  name         = "ams-fairplay-ask-hex"
  value        = var.amsDrm_FairPlay_Ask_Hex
  key_vault_id = azurerm_key_vault.shared_key_vault.id

  lifecycle {
    ignore_changes = [
      value,
      tags
    ]
  }
}

resource "azurerm_key_vault_secret" "ams_fair_play_certificate_b64" {
  name         = "ams-fairPlay-certificate-b64"
  value        = "KEY_VAULT_SECRET_NOT_SET"
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
  value = ["telestream-cloud-api-key", "grw-topic-end-point", "grw-topic-key", "ams-sp-client-id", "ams-sp-client-secret", "appinsights-instrumentationkey", "ams-fairplay-pfx-password", "ams-fairplay-ask-hex", "ams-fairPlay-certificate-b64"]
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
      days    = 30
    }
  }

  log {
    category = "PublishFailures"
    enabled  = true

    retention_policy {
      enabled = true
      days    = 30
    }
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      enabled = true
      days    = 30
    }
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

resource "local_file" "app_settings_keyvault_refs_json" {
  sensitive_content = jsonencode(local.functions_appsetting_keyvault_refs)
  filename          = "./app_settings/shared_keyvault_refs.json"
}
