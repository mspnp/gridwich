##################################################################################
# Main Terraform file
##################################################################################

locals {
  dev_environment_name = "dev"
  all_storage_accounts_ids = concat(
    module.sa_inbox.storage_account_ids,
    module.sa_main.storage_account_ids,
    module.sa_migrate.storage_account_ids,
    module.sa_outbox.storage_account_ids
  )
  all_storage_resource_groups_ids = concat(
    module.sa_inbox.resource_group_ids,
    module.sa_main.resource_group_ids,
    module.sa_migrate.resource_group_ids,
    module.sa_outbox.resource_group_ids
  )
  default_cors = [{
    allowed_headers    = ["*"]
    allowed_methods    = ["DELETE", "GET", "HEAD", "POST", "OPTIONS", "PUT"]
    allowed_origins    = ["https://extsys.something.com", "https://localhost:9002"]
    exposed_headers    = ["*"]
    max_age_in_seconds = 300
  }]
}

##################################################################################
# RESOURCES
##################################################################################

resource "azurerm_resource_group" "application" {
  name     = "${var.appname}-application-rg-${var.environment}"
  location = var.location
  tags = {
    environment     = var.environment
    department      = var.department
    appname         = var.appname
    functional_area = "application"
  }
}

resource "azurerm_resource_group" "shared" {
  name     = "${var.appname}-shared-rg-${var.environment}"
  location = var.location
  tags = {
    environment     = var.environment
    department      = var.department
    appname         = var.appname
    functional_area = "shared"
  }
}

# Storage accounts

module "sa_inbox" {
  source                  = "./storage-accounts"
  resource_group_name     = "inbox"
  only_one_resource_group = true
  accounts_amount         = 3
  accounts_details = [
    { account_name = "inbox", containers = ["onpremauto"], cors = [] },
    { account_name = "inboxpxy", containers = [], cors = local.default_cors }
  ]

  appname     = var.appname
  environment = var.environment
  location    = var.location
  tags = {
    environment     = var.environment
    department      = var.department
    appname         = var.appname
    functional_area = "inbox"
  }
}

module "sa_main" {
  source                  = "./storage-accounts"
  resource_group_name     = "main"
  only_one_resource_group = false
  accounts_amount         = 1
  accounts_details = [
    { account_name = "lts", containers = [], cors = [] },
    { account_name = "hdmez", containers = [], cors = [] },
    { account_name = "index", containers = [], cors = [] },
    { account_name = "asset", containers = [], cors = [] },
    { account_name = "encodetmp", containers = ["telestreamoutput"], cors = [] },
    { account_name = "strm", containers = [], cors = local.default_cors },
    { account_name = "proxy", containers = [], cors = local.default_cors },
    { account_name = "sprites", containers = [], cors = local.default_cors }
  ]

  appname     = var.appname
  environment = var.environment
  location    = var.location
  tags = {
    environment     = var.environment
    department      = var.department
    appname         = var.appname
    functional_area = "main"
  }
}

module "sa_migrate" {
  source                  = "./storage-accounts"
  resource_group_name     = "migrate"
  only_one_resource_group = true
  accounts_amount         = 1
  accounts_details = [
    { account_name = "migrate", containers = ["reachengine"], cors = [] }
  ]

  appname     = var.appname
  environment = var.environment
  location    = var.location
  tags = {
    environment     = var.environment
    department      = var.department
    appname         = var.appname
    functional_area = "migrate"
  }
}

module "sa_outbox" {
  source                  = "./storage-accounts"
  resource_group_name     = "outbox"
  only_one_resource_group = true
  accounts_amount         = 1
  accounts_details = [
    { account_name = "order", containers = [], cors = [] }
  ]

  appname     = var.appname
  environment = var.environment
  location    = var.location
  tags = {
    environment     = var.environment
    department      = var.department
    appname         = var.appname
    functional_area = "outbox"
  }
}

module "functions" {
  source              = "./functions"
  appname             = var.appname
  domainprefix        = var.domainprefix
  environment         = var.environment
  resource_group_name = azurerm_resource_group.application.name
  location            = azurerm_resource_group.application.location
}

module "shared" {
  source                       = "./shared"
  key_vault_tenant_id          = data.azurerm_client_config.current.tenant_id
  appname                      = var.appname
  access_policy_object_ids     = [data.azurerm_client_config.current.object_id]
  environment                  = var.environment
  domainprefix                 = var.domainprefix
  resource_group_name          = azurerm_resource_group.shared.name
  location                     = azurerm_resource_group.shared.location
  telestream_cloud_api_key           = var.telestream_cloud_api_key
  functions_principal_id       = module.functions.principal_id
  amsDrm_FairPlay_Pfx_Password = var.amsDrm_FairPlay_Pfx_Password
  amsDrm_FairPlay_Ask_Hex      = var.amsDrm_FairPlay_Ask_Hex
}

module "appinsights" {
  source              = "./appinsights"
  appname             = var.appname
  environment         = var.environment
  resource_group_name = azurerm_resource_group.application.name
  location            = azurerm_resource_group.application.location
  key_vault_id        = module.shared.shared_key_vault_id
  key_vault_name      = module.shared.shared_kv_name
}

module "secret_changed_handler" {
  source = "./secretchangedhandler"

  subscription_id            = data.azurerm_client_config.current.subscription_id
  resource_group_name        = azurerm_resource_group.shared.name
  location                   = azurerm_resource_group.shared.location
  appname                    = var.appname
  domainprefix               = var.domainprefix
  environment                = var.environment
  sp_client_id               = data.azurerm_client_config.current.client_id
  sp_client_secret           = var.sp_client_secret
  shared_key_vault_id        = module.shared.shared_key_vault_id
  function_app_id            = module.functions.function_app_id
  secret_names_to_watch      = module.shared.secrets_in_shared_keyvault
  log_analytics_workspace_id = module.shared.log_analytics_workspace_id
}

module "key_roller" {
  source = "./keyroller"

  subscription_id            = data.azurerm_client_config.current.subscription_id
  resource_group_name        = azurerm_resource_group.shared.name
  location                   = azurerm_resource_group.shared.location
  appname                    = var.appname
  domainprefix               = var.domainprefix
  environment                = var.environment
  sp_client_id               = data.azurerm_client_config.current.client_id
  sp_client_secret           = var.sp_client_secret
  event_grid_topic_id        = module.shared.delivery_topic_id
  event_grid_topic_endpoint  = module.shared.delivery_topic_endpoint
  event_grid_topic_key       = module.shared.delivery_topic_key
  cache_busting              = var.pipeline_build_id
  log_analytics_workspace_id = module.shared.log_analytics_workspace_id
}

module "event_viewer" {
  source               = "./eventviewer"
  create_resource      = true # always true for now, we won't want to create in prod
  event_viewer_appname = format("%s-%s-wa-viewer-%s", var.appname, var.domainprefix, var.environment)
  resource_group_name  = azurerm_resource_group.shared.name
  az_ad_domain         = var.az_ad_domain
  key_vault_name       = module.shared.shared_kv_name
  key_vault_id         = module.shared.shared_key_vault_id
}

module "functionKeys" {
  source              = "./functionKeys"
  function_app_name   = module.functions.function_app_name
  resource_group_name = azurerm_resource_group.application.name
  cache_busting       = var.pipeline_build_id
}

module "sub_extsys_to_topic" {
  source                     = "./egsubscription"
  endpoint                   = var.extsys_event_endpoint
  scope_ids                  = [module.shared.delivery_topic_id]
  name                       = "${var.appname}-topicextsys-egsub-${var.environment}"
  fail_gracefully            = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled     = var.run_flag_subscriptions_disabled
  dead_letter_sa_id          = module.shared.dead_letter_sa_id
  dead_letter_container_name = module.shared.dead_letter_container_name
}

module "sub_function_to_mediaservices" {
  source                 = "./egsubscription"
  endpoint               = "https://${module.functions.functionapp_endpoint_base}/api/${var.event_grid_function_name}?code=${module.functionKeys.host_key}"
  scope_ids              = [module.mediaservices.azurerm_media_services_account_resource_id]
  name                   = "${var.appname}-amsfxn-egsub-${var.environment}"
  fail_gracefully        = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled = var.run_flag_subscriptions_disabled
}

module "sub_function_to_topic" {
  source                 = "./egsubscription"
  endpoint               = "https://${module.functions.functionapp_endpoint_base}/api/${var.event_grid_function_name}?code=${module.functionKeys.host_key}"
  scope_ids              = [module.shared.delivery_topic_id]
  name                   = "${var.appname}-topicfxn-egsub-${var.environment}"
  fail_gracefully        = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled = var.run_flag_subscriptions_disabled
}

module "sub_function_to_storage_accounts" {
  source                 = "./egsubscription"
  endpoint               = "https://${module.functions.functionapp_endpoint_base}/api/${var.event_grid_function_name}?code=${module.functionKeys.host_key}"
  scope_ids              = local.all_storage_accounts_ids
  name                   = "${var.appname}-storagefxn-egsub-${var.environment}"
  fail_gracefully        = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled = var.run_flag_subscriptions_disabled
}

module "sub_viewer_to_mediaservices" {
  source                 = "./egsubscription"
  endpoint               = module.event_viewer.event_viewer_subscription_endpoint
  scope_ids              = [module.mediaservices.azurerm_media_services_account_resource_id]
  name                   = "${var.appname}-amsviewer-egsub-${var.environment}"
  fail_gracefully        = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled = var.run_flag_subscriptions_disabled
}

module "sub_viewer_to_topic" {
  source                 = "./egsubscription"
  endpoint               = module.event_viewer.event_viewer_subscription_endpoint
  scope_ids              = [module.shared.delivery_topic_id]
  name                   = "${var.appname}-topicviewer-egsub-${var.environment}"
  fail_gracefully        = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled = var.run_flag_subscriptions_disabled
}

module "sub_viewer_to_storage_accounts" {
  source                 = "./egsubscription"
  endpoint               = module.event_viewer.event_viewer_subscription_endpoint
  scope_ids              = local.all_storage_accounts_ids
  name                   = "${var.appname}-saviewer-egsub-${var.environment}"
  fail_gracefully        = var.run_flag_subscriptions_fail_gracefully
  subscriptions_disabled = var.run_flag_subscriptions_disabled
}

module "mediaservices" {
  source                                         = "./mediaservices"
  appname                                        = var.appname
  environment                                    = var.environment
  resource_group_name                            = azurerm_resource_group.application.name
  location                                       = azurerm_resource_group.application.location
  subscription_id                                = data.azurerm_client_config.current.subscription_id
  tenant_id                                      = data.azurerm_client_config.current.tenant_id
  key_vault_name                                 = module.shared.shared_kv_name
  scope_ids                                      = local.all_storage_accounts_ids
  ams_v2_callback_endpoint                       = "https://${module.functions.functionapp_endpoint_base}/api/${var.amsv2callback_function_name}?code=${module.functionKeys.host_key}"
  amsDrm_OpenIdConnectDiscoveryDocument_endpoint = var.amsDrm_OpenIdConnectDiscoveryDocument_endpoint
  amsDrm_EnableContentKeyPolicyUpdate            = var.amsDrm_EnableContentKeyPolicyUpdate
}

module "bash_script_generator" {
  source                                = "./bashscriptgenerator"
  logic_app_sch_service_principal_id    = module.secret_changed_handler.logic_app_service_principal_id
  logic_app_kr_service_principal_id     = module.key_roller.logic_app_service_principal_id
  function_app_id                       = module.functions.function_app_id
  media_services_name                   = module.mediaservices.azurerm_media_services_account_resource_name
  media_services_resource_group_name    = azurerm_resource_group.application.name
  media_services_account_resource_id    = module.mediaservices.azurerm_media_services_account_resource_id
  key_vault_name                        = module.shared.shared_kv_name
  storage_account_ids                   = local.all_storage_accounts_ids
  storage_resource_group_ids            = local.all_storage_resource_groups_ids
  function_principal_id                 = module.functions.principal_id
  tenant_id                             = data.azurerm_client_config.current.tenant_id
  event_grid_viewer_app_name            = module.event_viewer.event_viewer_appname
  event_grid_viewer_resource_group_name = module.event_viewer.event_viewer_resource_group_name
  pipeline_build_id                     = var.pipeline_build_id
}

# Add additional functional areas...
