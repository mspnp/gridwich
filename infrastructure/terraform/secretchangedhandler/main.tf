######################################################################################
# Logic App to watch for Secret changes in KeyVault and restart Function App
######################################################################################

# We construct the parameters to the Logic App like this because the template_deployment resource doesn't properly handle arrays today (azurerm v1.44)
locals {
  parameters_body = {
    workflows_bh_keyvaulthandler_name = {
      value = format("%s-%s-la-secretchangedhandler-%s", var.appname, var.domainprefix, var.environment)
    },
    vaults_bh_rollingvault_externalid = {
      value = var.shared_key_vault_id
    },
    subscriptionId = {
      value = var.subscription_id
    },
    diagnosticSettings_name = {
      value = "service"
    },
    log_analytics_workspace_id = {
      value = var.log_analytics_workspace_id
    },
    fxn_id = {
      value = var.function_app_id
    },
    keysToWatch = {
      value = var.secret_names_to_watch
    },
    client_id = {
      value = var.sp_client_id
    },
    client_secret = {
      value = var.sp_client_secret
    }
  }
}

resource "azurerm_resource_group_template_deployment" "logicapp" {
  name                = format("%s-%s-la-secretchangedhandler-%s", var.appname, var.domainprefix, var.environment)
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"
  parameters_content     = jsonencode(local.parameters_body)

  lifecycle {
    ignore_changes = [
      parameters_content,
	  template_content
    ]
  }

  template_content       = <<DEPLOY
{
	"$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
	"contentVersion": "1.0.0.0",
	"parameters": {
		"workflows_bh_keyvaulthandler_name": {
			"type": "String"
		},
		"vaults_bh_rollingvault_externalid": {
			"type": "String"
		},
		"subscriptionId": {
			"type": "String"
		},
		"location": {
			"defaultValue": "[resourceGroup().location]",
			"type": "String"
		},
		"diagnosticSettings_name": {
			"type": "String"
		},
		"log_analytics_workspace_id": {
			"type": "String"
		},
		"connections_azureeventgrid_name": {
			"defaultValue": "azureeventgrid",
			"type": "String"
		},
		"fxn_id": {
			"type": "String"
		},
		"keysToWatch" : {
			"type" : "array"
		},
		"client_id":{
			"type":"string"
		},
		"client_secret":{
			"type":"string"
		}
	},
	"variables": {},
	"resources": [
		{
			"type": "Microsoft.Web/connections",
			"apiVersion": "2016-06-01",
			"name": "[parameters('connections_azureeventgrid_name')]",
			"location": "[parameters('location')]",
			"properties": {
				"displayName": "Event Grid Connection",
				"customParameterValues": {},
				"api": {
					"id": "[concat('/subscriptions/', parameters('subscriptionId'), '/providers/Microsoft.Web/locations/', parameters('location'), '/managedApis/azureeventgrid')]"
				},
				"parameterValues": {
					"token:TenantId": "[subscription().tenantId]",
					"token:clientId": "[parameters('client_id')]",
					"token:clientSecret": "[parameters('client_secret')]",
					"token:grantType": "client_credentials"
				}
			}
		},
		{
			"type": "Microsoft.Logic/workflows",
			"apiVersion": "2017-07-01",
			"name": "[parameters('workflows_bh_keyvaulthandler_name')]",
			"dependsOn": [
				"[parameters('connections_azureeventgrid_name')]"
			],
			"location": "[parameters('location')]",
            "identity": {
                "type": "SystemAssigned"
            },
			"properties": {
				"state": "Enabled",
				"definition": {
					"$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
					"contentVersion": "1.0.0.0",
					"parameters": {
						"$connections": {
							"defaultValue": {},
							"type": "Object"
						}
					},
					"triggers": {
						"When_a_key_vault_key_is_updated": {
							"splitOn": "@triggerBody()",
							"type": "ApiConnectionWebhook",
							"inputs": {
								"body": {
									"properties": {
										"destination": {
											"endpointType": "webhook",
											"properties": {
												"endpointUrl": "@{listCallbackUrl()}"
											}
										},
										"filter": {
											"includedEventTypes": [
												"Microsoft.KeyVault.SecretNewVersionCreated"
											]
										},
										"topic": "[parameters('vaults_bh_rollingvault_externalid')]"
									}
								},
								"host": {
									"connection": {
										"name": "@parameters('$connections')['azureeventgrid']['connectionId']"
									}
								},
								"path": "[concat('/subscriptions/@{encodeURIComponent(''', parameters('subscriptionId'), ''')}/providers/@{encodeURIComponent(''Microsoft.KeyVault.vaults'')}/resource/eventSubscriptions')]",
								"queries": {
									"x-ms-api-version": "2017-06-15-preview"
								}
							}
						}
					},
					"actions": {
						"Condition": {
							"actions": {
								"Trigger_application_of_app_settings_(soft-restart_Function_App)": {
									"inputs": {
										"authentication": {
											"type": "ManagedServiceIdentity"
										},
										"method": "POST",
										"uri": "[concat('https://management.azure.com', parameters('fxn_id'), '/restart?softRestart=true&synchronous=true&api-version=2019-08-01')]"
									},
									"runAfter": {},
									"type": "Http"
								}
							},
							"expression": {
								"and": [
									{
										"contains": [
											"@variables('keysToWatch')",
											"@triggerBody()?['subject']"
										]
									}
								]
							},
							"runAfter": {
								"Init_keysToWatch": [
									"Succeeded"
								]
							},
							"type": "If"
						},
						"Init_keysToWatch": {
							"inputs": {
								"variables": [
									{
										"name": "keysToWatch",
										"type": "array",
										"value": "[parameters('keysToWatch')]"
									}
								]
							},
							"runAfter": {},
							"type": "InitializeVariable"
						}
					},
					"outputs": {}
				},
				"parameters": {
					"$connections": {
						"value": {
							"azureeventgrid": {
								"connectionId": "[resourceId('Microsoft.Web/connections', parameters('connections_azureeventgrid_name'))]",
								"connectionName": "azureeventgrid",
								"id": "[concat('/subscriptions/', parameters('subscriptionId'), '/providers/Microsoft.Web/locations/', parameters('location'), '/managedApis/azureeventgrid')]"
							}
						}
					}
				}
			},
			"resources": [
				{
					"type": "providers/diagnosticSettings",
					"name": "[concat('Microsoft.Insights/', parameters('diagnosticSettings_name'))]",
					"dependsOn": [
						"[parameters('workflows_bh_keyvaulthandler_name')]"
					],
					"apiVersion": "2017-05-01-preview",
					"properties": {
						"name": "[parameters('diagnosticSettings_name')]",
						"workspaceId": "[parameters('log_analytics_workspace_id')]",
						"logs": [
							{
								"category": "WorkflowRuntime",
								"enabled": true,
								"retentionPolicy": {
									"days": 0,
									"enabled": false
								}
							}
						],
						"metrics": [
							{
								"category": "AllMetrics",
								"enabled": false,
								"retentionPolicy": {
									"days": 0,
									"enabled": false
								}
							}
						]
					}
				}
			]
		}
	],
	"outputs": {
		"LogicAppServicePrincipalId": {
			"type": "string",
			"value": "[reference(concat('Microsoft.Logic/workflows/',parameters('workflows_bh_keyvaulthandler_name')), '2019-05-01', 'Full').identity.principalId]"
		}
	}
}
DEPLOY
}
