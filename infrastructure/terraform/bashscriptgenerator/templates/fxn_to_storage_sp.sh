#!/bin/bash

for acct in ${storageAccountIds}
{
    echo "Granting fxn access to $acct"
    az role assignment create --role "Storage Blob Data Contributor" --assignee-object-id ${functionPrincipalId} --scope $acct  --assignee-principal-type ServicePrincipal
}

for id in ${storageRgIds}
{
    echo "Granting fxn access to $id"
    az role assignment create --role "Reader and Data Access" --assignee-object-id ${functionPrincipalId} --scope $id  --assignee-principal-type ServicePrincipal
}