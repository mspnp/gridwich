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

resource "azurerm_app_service_plan" "fxnapp" {
  name                = format("%s-%s-fxn-asp-%s", var.appname, var.domainprefix, var.environment)
  location            = var.location
  resource_group_name = var.resource_group_name
  kind                = "functionapp"
  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_function_app" "fxn" {
  name                       = format("%s-%s-fxn-%s", var.appname, var.domainprefix, var.environment)
  location                   = var.location
  resource_group_name        = var.resource_group_name
  app_service_plan_id        = azurerm_app_service_plan.fxnapp.id
  storage_account_name       = azurerm_storage_account.fxnstor.name
  storage_account_access_key = azurerm_storage_account.fxnstor.primary_access_key
  version                    = "~3"
  https_only                 = true

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    ignore_changes = [
      app_settings
    ]
  }
}

resource "azurerm_function_app_slot" "fxnslot" {
  name                       = "source-slot"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  app_service_plan_id        = azurerm_app_service_plan.fxnapp.id
  function_app_name          = azurerm_function_app.fxn.name
  storage_account_name       = azurerm_storage_account.fxnstor.name
  storage_account_access_key = azurerm_storage_account.fxnstor.primary_access_key
  version                    = "~3"
  https_only                 = true
  
  lifecycle {
    ignore_changes = [
      app_settings
    ]
  }
}