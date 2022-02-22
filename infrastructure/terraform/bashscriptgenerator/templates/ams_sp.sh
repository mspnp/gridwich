#!/bin/bash

for id in ${mediaServicesAccountResourceId}
{
    echo "Granting fxn access to $id"
    az role assignment create --role "Contributor" --assignee-object-id ${functionPrincipalId} --scope $id --assignee-principal-type ServicePrincipal
    az role assignment create --role "Media Services Live Events Administrator" --assignee-object-id ${functionPrincipalId} --scope $id --assignee-principal-type ServicePrincipal
    az role assignment create --role "Media Services Media Operator" --assignee-object-id ${functionPrincipalId} --scope $id --assignee-principal-type ServicePrincipal
}

# Ref: https://docs.microsoft.com/en-us/azure/media-services/latest/access-api-cli-how-to

echo 'Creating service principal for Azure Media Services'
AZOUT=$(az ams account sp create --account-name ${mediaServicesName} --resource-group ${mediaServicesResourceGroupName} | jq '{AadClientId: .AadClientId, AadSecret:.AadSecret}')
echo 'Adding access policy in KeyVault'
USER_PRINCIPAL_NAME=$(az ad signed-in-user show | jq -r '.userPrincipalName')
az keyvault set-policy --name ${keyVaultName} --upn $USER_PRINCIPAL_NAME --secret-permissions set get list delete > /dev/null
echo 'Updating ams-sp-client-id and ams-sp-client-secret in KeyVault'
az keyvault secret set --vault-name ${keyVaultName} --name 'ams-sp-client-id' --value $(echo $AZOUT | jq -r '.AadClientId') > /dev/null
az keyvault secret set --vault-name ${keyVaultName} --name 'ams-sp-client-secret' --value $(echo $AZOUT | jq -r '.AadSecret')  > /dev/null
echo 'Revoking access policy in KeyVault'
az keyvault delete-policy --name ${keyVaultName} --upn $USER_PRINCIPAL_NAME > /dev/null
echo 'Done.'