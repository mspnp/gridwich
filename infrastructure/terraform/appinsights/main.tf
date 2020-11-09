##################################################################################
# RESOURCES
##################################################################################

resource "azurerm_application_insights" "logging" {
  name                = format("%s-ai-%s", var.appname, var.environment)
  location            = var.location
  resource_group_name = var.resource_group_name
  application_type    = "web"
}

#############################
# Secrets
#############################

resource "azurerm_key_vault_secret" "logging_app_insights_key" {
  name         = "appinsights-instrumentationkey"
  value        = azurerm_application_insights.logging.instrumentation_key
  key_vault_id = var.key_vault_id
}


##################################################################################
# Functions App Settings Terraform file
##################################################################################

locals {
  functions_appsetting = [
    {
      name        = "APPINSIGHTS_INSTRUMENTATIONKEY"
      value       = format("@Microsoft.KeyVault(SecretUri=https://%s.vault.azure.net/secrets/%s/)", var.key_vault_name, azurerm_key_vault_secret.logging_app_insights_key.name)
      slotSetting = false
    },
    {
      name        = "AppInsights_ResourceId"
      value       = urlencode(azurerm_application_insights.logging.id)
      slotSetting = false
    }
  ]
}

resource "local_file" "app_settings" {
  sensitive_content = jsonencode(local.functions_appsetting)
  filename          = "./app_settings/appinsights_appsettings.json"
}
