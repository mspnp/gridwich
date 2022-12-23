#!/bin/bash

for id in ${mediaServicesAccountResourceId}
{
    echo "Granting fxn access to $id"
    az role assignment create --role "Contributor" --assignee-object-id ${functionPrincipalId} --scope $id --assignee-principal-type ServicePrincipal
    # az role assignment create --role "Media Services Live Events Administrator" --assignee-object-id ${functionPrincipalId} --scope $id --assignee-principal-type ServicePrincipal
    # az role assignment create --role "Media Services Media Operator" --assignee-object-id ${functionPrincipalId} --scope $id --assignee-principal-type ServicePrincipal
}

# Ref: https://learn.microsoft.com/en-us/azure/media-services/previous/media-services-use-aad-auth-to-access-ams-api
