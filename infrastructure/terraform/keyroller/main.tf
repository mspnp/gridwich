######################################################################################
# Logic App to roll keys on storage accounts when requested
######################################################################################

# We construct the parameters to the Logic App like this because the template_deployment resource doesn't properly handle arrays today (azurerm v1.44)
locals {
  keyroller_parameters_body = {
    workflow_name = {
      value = format("%s-%s-la-keyroller-%s", var.appname, var.domainprefix, var.environment)
    },
    trigger_topic_id = {
      value = var.event_grid_topic_id
    },
    topic_endpoint = {
      value = var.event_grid_topic_endpoint
    },
    topic_token = {
      value = var.event_grid_topic_key
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
    client_id = {
      value = var.sp_client_id
    },
    client_secret = {
      value = var.sp_client_secret
    }
  }
}

resource "azurerm_template_deployment" "keyroller_logicapp" {
  name                = "${local.keyroller_parameters_body.workflow_name.value}-${var.cache_busting}"
  resource_group_name = var.resource_group_name
  deployment_mode     = "Incremental"
  parameters_body     = jsonencode(local.keyroller_parameters_body)
  template_body       = <<DEPLOY
{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "workflow_name": {
            "type": "String"
        },
        "trigger_topic_id": {
            "type": "String"
        },
        "topic_endpoint": {
            "type": "String"
        },
        "topic_token": {
            "type": "String"
        },
        "connections_azureeventgrid_name": {
			"defaultValue": "azureeventgrid",
            "type": "String"
        },
        "connections_azureeventgridpublish_name": {
			"defaultValue": "azureeventgridpublish",
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
                "displayName": "Event Grid Trigger Connection",
                "customParameterValues": {},
                "api": {
					"id": "[concat('/subscriptions/', parameters('subscriptionId'), '/providers/Microsoft.Web/locations/', parameters('location'), '/managedApis/', parameters('connections_azureeventgrid_name'))]"
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
            "type": "Microsoft.Web/connections",
            "apiVersion": "2016-06-01",
            "name": "[parameters('connections_azureeventgridpublish_name')]",
			"location": "[parameters('location')]",
            "properties": {
                "displayName": "Event Grid Publish Connection",
                "customParameterValues": {},
                "api": {
					"id": "[concat('/subscriptions/', parameters('subscriptionId'), '/providers/Microsoft.Web/locations/', parameters('location'), '/managedApis/', parameters('connections_azureeventgridpublish_name'))]"
                },
				"parameterValues": {
					"endpoint": "[parameters('topic_endpoint')]",
					"api_key": "[parameters('topic_token')]"
				}
            }
        },
		{
            "type": "Microsoft.Logic/workflows",
            "apiVersion": "2017-07-01",
            "name": "[parameters('workflow_name')]",
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
                        "When_a_roll_storage_key_event_is_received_from_the_External_System": {
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
                                                "request.rollkey.storage"
                                            ]
                                        },
                                        "topic": "[parameters('trigger_topic_id')]"
                                    }
                                },
                                "host": {
                                    "connection": {
                                        "name": "@parameters('$connections')['azureeventgrid']['connectionId']"
                                    }
                                },
                                "path": "[concat('/subscriptions/@{encodeURIComponent(''', parameters('subscriptionId'), ''')}/providers/@{encodeURIComponent(''Microsoft.EventGrid.Topics'')}/resource/eventSubscriptions')]",
                                "queries": {
                                    "x-ms-api-version": "2017-06-15-preview"
                                }
                            }
                        }
                    },
                    "actions": {
                        "Filter_for_target_storage_account": {
                            "runAfter": {
                                "Parse_JSON": [
                                    "Succeeded"
                                ],
                                "Parse_storage_account_list": [
                                    "Succeeded"
                                ]
                            },
                            "type": "Query",
                            "inputs": {
                                "from": "@body('Parse_storage_account_list')?['value']",
                                "where": "@equals(item()?['name'], body('Parse_JSON')?['account'])"
                            }
                        },
                        "Get_all_storage_accounts_for_subscription": {
                            "runAfter": {},
                            "type": "Http",
                            "inputs": {
                                "authentication": {
                                    "type": "ManagedServiceIdentity"
                                },
                                "method": "GET",
                                "uri": "[concat('https://management.azure.com/subscriptions/@{encodeURIComponent(''', parameters('subscriptionId'), ''')}/providers/Microsoft.Storage/storageAccounts?api-version=2019-06-01')]"
                            }
                        },
                        "If_account_found_in_sub": {
                            "actions": {
                                "For_each": {
                                    "foreach": "@body('Filter_for_target_storage_account')",
                                    "actions": {
                                        "Publish_failure_event": {
                                            "runAfter": {
                                                "Set_failed_1": [
                                                    "Succeeded"
                                                ]
                                            },
                                            "type": "ApiConnection",
                                            "inputs": {
                                                "body": [
                                                    {
                                                        "data": {
                                                            "account": "@{body('Parse_JSON')?['account']}",
                                                            "error": "@{variables('error')}",
                                                            "keyName": "@{body('Parse_JSON')?['keyName']}"
                                                        },
                                                        "eventType": "response.rollkey.storage.failure",
                                                        "id": "@{guid()}",
                                                        "subject": "/rollkey/failure"
                                                    }
                                                ],
                                                "host": {
                                                    "connection": {
                                                        "name": "@parameters('$connections')['azureeventgridpublish']['connectionId']"
                                                    }
                                                },
                                                "method": "post",
                                                "path": "/eventGrid/api/events"
                                            }
                                        },
                                        "Publish_success_event": {
                                            "runAfter": {
                                                "Regenerate_Storage_Key": [
                                                    "Succeeded"
                                                ]
                                            },
                                            "type": "ApiConnection",
                                            "inputs": {
                                                "body": [
                                                    {
                                                        "data": {
                                                            "account": "@{body('Parse_JSON')?['account']}",
                                                            "keyName": "@{body('Parse_JSON')?['keyName']}"
                                                        },
                                                        "eventType": "response.rollkey.storage.success",
                                                        "id": "@{guid()}",
                                                        "subject": "/rollkey/success"
                                                    }
                                                ],
                                                "host": {
                                                    "connection": {
                                                        "name": "@parameters('$connections')['azureeventgridpublish']['connectionId']"
                                                    }
                                                },
                                                "method": "post",
                                                "path": "/eventGrid/api/events"
                                            }
                                        },
                                        "Regenerate_Storage_Key": {
                                            "runAfter": {},
                                            "type": "Http",
                                            "inputs": {
                                                "authentication": {
                                                    "type": "ManagedServiceIdentity"
                                                },
                                                "body": {
                                                    "keyName": "@{body('Parse_JSON')?['keyName']}"
                                                },
                                                "method": "POST",
                                                "uri": "https://management.azure.com@{items('For_each')?['id']}/regenerateKey?api-version=2019-06-01"
                                            },
                                            "runtimeConfiguration": {
                                                "secureData": {
                                                    "properties": [
                                                        "outputs"
                                                    ]
                                                }
                                            }
                                        },
                                        "Set_failed_1": {
                                            "runAfter": {
                                                "Regenerate_Storage_Key": [
                                                    "Failed",
                                                    "Skipped",
                                                    "TimedOut"
                                                ]
                                            },
                                            "type": "SetVariable",
                                            "inputs": {
                                                "name": "error",
                                                "value": "@{body('Regenerate_Storage_Key')}"
                                            }
                                        }
                                    },
                                    "runAfter": {},
                                    "type": "Foreach",
                                    "description": "While this is in a ForEach loop, it will only be executed once",
                                    "runtimeConfiguration": {
                                        "concurrency": {
                                            "repetitions": 1
                                        }
                                    }
                                }
                            },
                            "runAfter": {
                                "Filter_for_target_storage_account": [
                                    "Succeeded"
                                ]
                            },
                            "else": {
                                "actions": {
                                    "Publish_failure_event_2": {
                                        "runAfter": {
                                            "Set_failed_2": [
                                                "Succeeded"
                                            ]
                                        },
                                        "type": "ApiConnection",
                                        "inputs": {
                                            "body": [
                                                {
                                                    "data": {
                                                        "account": "@{body('Parse_JSON')?['account']}",
                                                        "error": "@{variables('error')}",
                                                        "keyName": "@{body('Parse_JSON')?['keyName']}"
                                                    },
                                                    "eventType": "response.rollkey.storage.failure",
                                                    "id": "@{guid()}",
                                                    "subject": "/rollkey/failure"
                                                }
                                            ],
                                            "host": {
                                                "connection": {
                                                    "name": "@parameters('$connections')['azureeventgridpublish']['connectionId']"
                                                }
                                            },
                                            "method": "post",
                                            "path": "/eventGrid/api/events"
                                        }
                                    },
                                    "Set_failed_2": {
                                        "runAfter": {},
                                        "type": "SetVariable",
                                        "inputs": {
                                            "name": "error",
                                            "value": "Account not found in subscription"
                                        }
                                    }
                                }
                            },
                            "expression": {
                                "and": [
                                    {
                                        "equals": [
                                            "@length(body('Filter_for_target_storage_account'))",
                                            1
                                        ]
                                    }
                                ]
                            },
                            "type": "If"
                        },
                        "If_has_error,_terminate_with_failed_status": {
                            "actions": {
                                "Terminate": {
                                    "runAfter": {},
                                    "type": "Terminate",
                                    "inputs": {
                                        "runError": {
                                            "message": "@variables('error')"
                                        },
                                        "runStatus": "Failed"
                                    }
                                }
                            },
                            "runAfter": {
                                "If_account_found_in_sub": [
                                    "Succeeded"
                                ]
                            },
                            "expression": {
                                "and": [
                                    {
                                        "greater": [
                                            "@length(variables('error'))",
                                            0
                                        ]
                                    }
                                ]
                            },
                            "type": "If"
                        },
                        "Init_error_var": {
                            "runAfter": {},
                            "type": "InitializeVariable",
                            "inputs": {
                                "variables": [
                                    {
                                        "name": "error",
                                        "type": "string"
                                    }
                                ]
                            }
                        },
                        "Parse_JSON": {
                            "runAfter": {
                                "Init_error_var": [
                                    "Succeeded"
                                ]
                            },
                            "type": "ParseJson",
                            "inputs": {
                                "content": "@triggerBody()?['data']",
                                "schema": {
                                    "properties": {
                                        "account": {
                                            "type": "string"
                                        },
                                        "keyName": {
                                            "type": "string"
                                        }
                                    },
                                    "type": "object"
                                }
                            }
                        },
                        "Parse_storage_account_list": {
                            "runAfter": {
                                "Get_all_storage_accounts_for_subscription": [
                                    "Succeeded"
                                ]
                            },
                            "type": "ParseJson",
                            "inputs": {
                                "content": "@body('Get_all_storage_accounts_for_subscription')",
                                "schema": {
                                    "properties": {
                                        "value": {
                                            "items": {
                                                "properties": {
                                                    "id": {
                                                        "type": "string"
                                                    },
                                                    "kind": {
                                                        "type": "string"
                                                    },
                                                    "location": {
                                                        "type": "string"
                                                    },
                                                    "name": {
                                                        "type": "string"
                                                    },
                                                    "properties": {
                                                        "properties": {},
                                                        "type": "object"
                                                    },
                                                    "sku": {
                                                        "properties": {
                                                            "name": {
                                                                "type": "string"
                                                            },
                                                            "tier": {
                                                                "type": "string"
                                                            }
                                                        },
                                                        "type": "object"
                                                    },
                                                    "tags": {
                                                        "properties": {
                                                            "key1": {
                                                                "type": "string"
                                                            },
                                                            "key2": {
                                                                "type": "string"
                                                            }
                                                        },
                                                        "type": "object"
                                                    },
                                                    "type": {
                                                        "type": "string"
                                                    }
                                                },
                                                "required": [
                                                    "id",
                                                    "name",
                                                    "location",
                                                    "tags",
                                                    "type",
                                                    "properties",
                                                    "sku",
                                                    "kind"
                                                ],
                                                "type": "object"
                                            },
                                            "type": "array"
                                        }
                                    },
                                    "type": "object"
                                }
                            }
                        }
                    },
                    "outputs": {}
                },
                "parameters": {
                    "$connections": {
                        "value": {
                            "azureeventgrid": {
								"connectionId": "[resourceId('Microsoft.Web/connections', parameters('connections_azureeventgrid_name'))]",
                                "connectionName": "[parameters('connections_azureeventgrid_name')]",
								"id": "[concat('/subscriptions/', parameters('subscriptionId'), '/providers/Microsoft.Web/locations/', parameters('location'), '/managedApis/azureeventgrid')]"
                            },
                            "azureeventgridpublish": {
								"connectionId": "[resourceId('Microsoft.Web/connections', parameters('connections_azureeventgridpublish_name'))]",
                                "connectionName": "[parameters('connections_azureeventgridpublish_name')]",
								"id": "[concat('/subscriptions/', parameters('subscriptionId'), '/providers/Microsoft.Web/locations/', parameters('location'), '/managedApis/azureeventgridpublish')]"
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
						"[parameters('workflow_name')]"
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
			"value": "[reference(concat('Microsoft.Logic/workflows/',parameters('workflow_name')), '2019-05-01', 'Full').identity.principalId]"
		}
	}
}
DEPLOY
}
