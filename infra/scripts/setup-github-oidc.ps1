# Creates an Entra app + federated credential for GitHub Actions OIDC,
# an Entra group used as Azure SQL administrator, and scoped Azure RBAC.
#
# Prerequisites: Azure CLI logged in (az login) with rights to create apps and assign roles.
# Usage:  ./infra/scripts/setup-github-oidc.ps1

$ErrorActionPreference = 'Stop'

$repo = 'hemantshelar/asha-nandanvan'
$appName = 'github-ashanandanvan-dev'
$groupName = 'ashanandanvan-sql-admins-dev'
$resourceGroup = 'ashanandanvan-dev'
$sqlResourceGroup = 'invitation-web-group'
$location = 'australiaeast'
$environment = 'dev'

$account = az account show | ConvertFrom-Json
$subscriptionId = $account.id
$tenantId = $account.tenantId
Write-Host "Subscription $subscriptionId  Tenant $tenantId"

$app = az ad app list --display-name $appName --query '[0]' | ConvertFrom-Json
if (-not $app) {
    $app = az ad app create --display-name $appName | ConvertFrom-Json
}
$clientId = $app.appId
Write-Host "App client id $clientId"

$sp = az ad sp list --filter "appId eq '$clientId'" --query '[0]' | ConvertFrom-Json
if (-not $sp) {
    $sp = az ad sp create --id $clientId | ConvertFrom-Json
}

$subject = "repo:${repo}:environment:${environment}"
$existingFed = az ad app federated-credential list --id $clientId --query "[?name=='github-dev']" | ConvertFrom-Json
if (-not $existingFed) {
    $fed = @{
        name        = 'github-dev'
        issuer      = 'https://token.actions.githubusercontent.com'
        subject     = $subject
        audiences   = @('api://AzureADTokenExchange')
        description = 'GitHub Actions environment dev'
    } | ConvertTo-Json -Compress
    $fedFile = Join-Path $env:TEMP 'ashanandanvan-fed.json'
    Set-Content -Path $fedFile -Value $fed -Encoding utf8
    az ad app federated-credential create --id $clientId --parameters $fedFile | Out-Null
}

$group = az ad group list --display-name $groupName --query '[0]' | ConvertFrom-Json
if (-not $group) {
    $group = az ad group create --display-name $groupName --mail-nickname $groupName | ConvertFrom-Json
}

$currentUser = az ad signed-in-user show | ConvertFrom-Json
az ad group member add --group $group.id --member-id $currentUser.id 2>$null
az ad group member add --group $group.id --member-id $sp.id 2>$null

az group create --name $resourceGroup --location $location --tags application=ashanandanvan environment=dev | Out-Null

az role assignment create --assignee $sp.id --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup" 2>$null
az role assignment create --assignee $sp.id --role Contributor --scope "/subscriptions/$subscriptionId/resourceGroups/$sqlResourceGroup" 2>$null

Write-Host ''
Write-Host 'Set these GitHub Actions variables on environment "dev":'
Write-Host "  AZURE_CLIENT_ID              = $clientId"
Write-Host "  AZURE_TENANT_ID              = $tenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID        = $subscriptionId"
Write-Host "  SQL_ENTRA_ADMIN_LOGIN        = $groupName"
Write-Host "  SQL_ENTRA_ADMIN_OBJECT_ID    = $($group.id)"
Write-Host ''
Write-Host 'Set these GitHub Actions secrets on environment "dev":'
Write-Host '  GOOGLE_CLIENT_ID'
Write-Host '  GOOGLE_CLIENT_SECRET'
Write-Host '  SQUARE_APPLICATION_ID'
Write-Host '  SQUARE_ACCESS_TOKEN'
Write-Host '  SQUARE_LOCATION_ID'
Write-Host '  ADMIN_SEED_EMAIL'
Write-Host ''
Write-Host 'Also add this Google redirect URI:'
Write-Host '  https://app-ashanandanvan-dev.azurewebsites.net/signin-google'
