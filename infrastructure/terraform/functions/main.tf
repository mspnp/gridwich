##################################################################################
# Function App Terraform file 
##################################################################################

resource "azurerm_storage_account" "fxnstor" {
  name                     = format("%sfxnssa%s%s", var.appname, var.domainprefix, var.environment)
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
}

resource "azurerm_service_plan" "fxnapp" {
  name                = format("%s-%s-fxn-asp-%s", var.appname, var.domainprefix, var.environment)
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Windows"
  sku_name            = "Y1"
}

resource "azurerm_windows_function_app" "fxn" {
  name                       = format("%s-%s-fxn-%s", var.appname, var.domainprefix, var.environment)
  location                   = var.location
  resource_group_name        = var.resource_group_name
  service_plan_id            = azurerm_service_plan.fxnapp.id
  storage_account_name       = azurerm_storage_account.fxnstor.name
  storage_account_access_key = azurerm_storage_account.fxnstor.primary_access_key
  functions_extension_version = "~4"
  https_only                  = true
  app_settings = {
    FUNCTIONS_WORKER_RUNTIME = "dotnet"
  }
  site_config {
  }
  identity {
    type = "SystemAssigned"
  }
  lifecycle {
    ignore_changes = [
      app_settings,
      site_config
    ]
  }
}

resource "azurerm_windows_function_app_slot" "fxnslot" {
  name                       = "source-slot"
  function_app_id            = azurerm_windows_function_app.fxn.id
  storage_account_name       = azurerm_storage_account.fxnstor.name
  storage_account_access_key = azurerm_storage_account.fxnstor.primary_access_key
  functions_extension_version = "~4"
  https_only                  = true
  app_settings = {
    FUNCTIONS_WORKER_RUNTIME = "dotnet"
  }
  site_config {
  }
  identity {
      type = "SystemAssigned"
  }
  lifecycle {
    ignore_changes = [
      app_settings,
      site_config
    ]
  }
}