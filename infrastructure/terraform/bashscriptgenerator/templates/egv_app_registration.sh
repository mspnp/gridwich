#!/bin/bash

set -eu

#########################################################################################################################################
#- Purpose: Script is used to create an Az AD App Registration & use this App Registration to enable authe/autho on the event grid viewer
#- Terraform variables needed:
#- tenantId - The Az AD tenantid, which is used to create the token issurer url
#- eventgridViewerResourceGroupName - The event grid viewer resource group name
#- eventgridViewerAppName - The event grid viewer web app name
#- pipelineBuildId - The pipline build id
#- keyVaultName - The Az Keyvault to store the Az AD App Registration AppId/ClientId
#- Note: This script expects to find the "egv_app_registration_manifest.json" file in the same directory as this script
#########################################################################################################################################

# GUID references in egv_app_registration_manifest.json
# https://github.com/mjisaak/azure-active-directory/blob/master/README.md#well-known-appids

# Per Microsoft, best practice to use a manifest file
# https://github.com/Azure/azure-cli/issues/6023#issuecomment-400011467

#######################################################
#- function used to print messages
#######################################################
function print() {
    echo "$1...."
}

# declare variables
declare FILE=egv_app_registration_manifest.json
declare secretName="sct-egvb-azad-client-id"

print "Checking to see if the egv_app_registration_manifest.json exists"
if [ ! -f "$FILE" ]; then
    print "$FILE does not exist."
    exit 1
fi

# get configuration value
print "Creating Managed Identity & Azure AD App Registration secret"
managedIdentityPrincipalId=$(az webapp identity assign -n ${eventgridViewerAppName} -g ${eventgridViewerResourceGroupName} -o json | jq -r '.principalId')
signedInUserPrincipalName=$(az ad signed-in-user show -o json | jq -r '.userPrincipalName')

# create app registration
print "Create new app registration for ${eventgridViewerAppName}"
appId=$(az ad app create --display-name ${eventgridViewerAppName} \
    --web-redirect-uris https://${eventgridViewerAppName}.azurewebsites.net/signin-oidc \
    --enable-id-token-issuance \
    --key-type Password \
    --required-resource-accesses @egv_app_registration_manifest.json \
    -o json \
    | jq -r '.appId')

# set access policies
print "Setting the keyvault access policies"
az keyvault set-policy --name ${keyVaultName} --upn "$signedInUserPrincipalName" --secret-permissions set get list delete -o none
az keyvault set-policy --name ${keyVaultName} --object-id "$managedIdentityPrincipalId" --secret-permissions set get list -o none

# setting keyvault secrets
print "Setting keyvault secrets"
az keyvault secret set --vault-name ${keyVaultName} -n "$secretName" --value "$appId" -o none

# restart the web app
print "Restart ${eventgridViewerAppName} web app"
az webapp stop  --name ${eventgridViewerAppName} \
    --resource-group ${eventgridViewerResourceGroupName} \
    -o none

az webapp start  --name ${eventgridViewerAppName} \
    --resource-group ${eventgridViewerResourceGroupName} \
    -o none

# provide output
print "App Registration AppId: $appId"
