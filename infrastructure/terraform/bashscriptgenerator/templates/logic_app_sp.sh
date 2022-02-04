#!/bin/bash

az role assignment create --role "Website Contributor" --assignee-object-id ${logicAppSCHServicePrincipalId} --scope ${functionAppId} --assignee-principal-type ServicePrincipal

for acct in ${storageAccountIds}
{
    echo "Granting Logic App access to $acct"
    az role assignment create --role "Storage Account Key Operator Service Role" --assignee-object-id ${logicAppKRServicePrincipalId} --scope $acct --assignee-principal-type ServicePrincipal
}

for id in ${storageRgIds}
{
    echo "Granting Logic App access to $id"
    az role assignment create --role "Reader and Data Access" --assignee-object-id ${logicAppKRServicePrincipalId} --scope $id --assignee-principal-type ServicePrincipal
}