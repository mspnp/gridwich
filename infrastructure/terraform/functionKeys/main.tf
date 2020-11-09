##################################################################################
# Function Keys Terraform file 
##################################################################################

# Get the functions keys out of the app
resource "azurerm_template_deployment" "function_keys" {
  name = "getFunctionAppHostKey"
  parameters = {
    "functionApp"  = var.function_app_name
    "cacheBusting" = var.cache_busting
  }
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"

  template_body = <<BODY
  {
      "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
      "contentVersion": "1.0.0.0",
      "parameters": {
          "functionApp" : {"type": "string", "defaultValue": ""},
          "cacheBusting": {"type": "string", "defaultValue": ""}
      },
      "variables": {
          "functionAppId": "[resourceId('Microsoft.Web/sites', parameters('functionApp'))]",
          "cacheBusting": "parameters('cacheBusting')"
      },
      "resources": [
      ],
      "outputs": {
          "functionkey": {
              "type": "string",
              "value": "[listkeys(concat(variables('functionAppId'), '/host/default'), '2018-11-01').functionKeys.default]"                                                                                
            }
       }
  }
  BODY
}