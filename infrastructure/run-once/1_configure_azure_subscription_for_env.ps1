param([string] $subid = "5eec61d7-ed64-4048-a3bd-133bfef1d7a2")

# Review and set the following before running the script:
Write-Output "Setting params"

$SUBSCRIPTIONID = $subid

###########################################
##  Login and set resource names
###########################################
##
## This section is a generic login

$ErrorActionPreference = "Stop"
try { 
    Write-Output "Set the active subscription"
    az account set --subscription $SUBSCRIPTIONID 
    if ($? -eq $False) { Write-Error -Message "Unable to set the active subscription" }
} 
catch {
    Write-Output "Attempt Login"
    $ErrorActionPreference = "Continue" # az login will throw errors regarding opening a browser, we want to Contnue.
    az login | Out-Null
    Write-Output "Set the active subscription"
    $ErrorActionPreference = "Stop"
    az account set --subscription $SUBSCRIPTIONID
    if ($? -eq $False) { Write-Error -Message "Unable to set the active subscription" }	 
}
Write-Output "Setting subscription and user information"
$ErrorActionPreference = "Stop"
$accountShowOutput = az account show | ConvertFrom-Json
$subscriptionId = $accountShowOutput.id
$subscriptionName = $accountShowOutput.name
$azureAdTenant = $accountShowOutput.tenantId
$signedInUserOutput = az ad signed-in-user show | ConvertFrom-Json
$signedInUserUpn = $signedInUserOutput.userPrincipalName
Write-Output "Using Subscription:" $accountShowOutput 
Write-Output "UserUpn" $signedInUserUpn

###########################################
##  Run Once Azure Admin Actions
###########################################
##
## The following Actions will be taken by this script:
##
## A0. Enable Event Grid resource provider for Azure Subscription.
## A1. TBD
## A2. TBD
##

## R0. Enable Event Grid resource provider for Azure Subscription.

Write-Output "A0. Registering Azure Subscription for Event Grid resource providers"
$ErrorActionPreference = "Continue" # DEV NOTE:  In case the next line to throws an error, we want to Continue dispite an error.
$eventGridProviderOutput = az provider show --namespace Microsoft.EventGrid --query "registrationState"
$ErrorActionPreference = "Stop" # DEV NOTE:  Now that we are past any known/expected error, we are strict about any errors thrown.  This pattern is used for checking existance throughout the script.
if ($eventGridProviderOutput -eq '"Registered"') { 
    Write-Output "NOTE: Event Grid functionality found already enabled";  
}
else {
    # Attempt to enable it, and check if succeeded
    az provider register --namespace Microsoft.EventGrid
    if ($? -eq $False) { Write-Error "Event Grid functionality failed to register" }
    $eventGridProviderOutput = az provider show --namespace Microsoft.EventGrid --query "registrationState"
}
Write-Output "A0. Azure Subscription for Event Grid resource providers registrationState is: $eventGridProviderOutput"
