##################################################################################
# Function Keys Terraform file 
##################################################################################

# Get the functions keys out of the app
resource "azurerm_resource_group_template_deployment" "function_keys" {
  name = "getFunctionAppHostKey"
 
  parameters_content = jsonencode({
    "functionApp" = {
      value = var.function_app_name
    },
    "cacheBusting" = {
      value = var.cache_busting
    }
  })
  
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"

  lifecycle {
    ignore_changes = [
      template_content,
      parameters_content
    ]
  }

  template_content = <<BODY
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