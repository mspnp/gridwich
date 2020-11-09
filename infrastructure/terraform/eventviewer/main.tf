data "http" "deployjson" {
  url = "https://raw.githubusercontent.com/Azure-Samples/eventgrid-viewer-blazor/main/infrastructure/arm/azuredeploy.json"
}

resource "azurerm_template_deployment" "event_grid_deploy" {
  count               = var.create_resource ? 1 : 0
  name                = "${var.event_viewer_appname}-deployment"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"
  template_body       = data.http.deployjson.body

  parameters = {
    siteName        = var.event_viewer_appname
    hostingPlanName = "${var.event_viewer_appname}-asp"
    sku             = "B1"
    enableAuth      = "true"
    keyvaultName    = var.key_vault_name
    azAdDomain      = var.az_ad_domain
  }
}