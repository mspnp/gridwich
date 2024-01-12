data "http" "deployjson" {
  url = "https://raw.githubusercontent.com/Azure-Samples/eventgrid-viewer-blazor/main/infrastructure/arm/azuredeploy.json"
}

resource "azurerm_resource_group_template_deployment" "event_grid_deploy" {
  count               = var.create_resource ? 1 : 0
  name                = "${var.event_viewer_appname}-deployment"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"
  template_content    =  data.http.deployjson.body

  parameters_content = jsonencode({
    "siteName" = {
      value = var.event_viewer_appname
    },
    "hostingPlanName" = {
      value = "${var.event_viewer_appname}-asp"
    },
     "sku" = {
      value = "B1"
    },
    "enableAuth" = {
      value = "true"
    },
     "keyvaultName" = {
      value = var.key_vault_name
    },
    "azAdDomain" = {
      value = var.entraid_domain
    }
  })

  lifecycle {
    ignore_changes = [
      parameters_content,
      template_content
    ]
  }
}