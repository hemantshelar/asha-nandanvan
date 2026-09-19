# Lists and recreates the GitHub Actions federated credential on
# github-ashanandanvan-dev. Run while logged into Azure subscription 1.

$ErrorActionPreference = 'Stop'

$appName = 'github-ashanandanvan-dev'
$subject = 'repo:hemantshelar/asha-nandanvan:environment:dev'

$app = az ad app list --display-name $appName --query '[0]' | ConvertFrom-Json
if (-not $app) {
    throw "App $appName was not found. Run setup-github-oidc.ps1 first."
}

Write-Host "App display name : $($app.displayName)"
Write-Host "App client id    : $($app.appId)"
Write-Host "App object id    : $($app.id)"
Write-Host "Expected subject : $subject"
Write-Host ''
Write-Host 'Current federated credentials:'
az ad app federated-credential list --id $app.id -o table

$existing = az ad app federated-credential list --id $app.id | ConvertFrom-Json
foreach ($cred in @($existing)) {
    if ($cred.subject -eq $subject -or $cred.name -eq 'github-env-dev' -or $cred.name -eq 'github-dev') {
        Write-Host "Removing $($cred.name) ($($cred.subject))"
        az ad app federated-credential delete --id $app.id --federated-credential-id $cred.id
    }
}

$fed = @{
    name        = 'github-env-dev'
    issuer      = 'https://token.actions.githubusercontent.com'
    subject     = $subject
    audiences   = @('api://AzureADTokenExchange')
    description = 'GitHub Actions environment dev'
} | ConvertTo-Json -Compress

$fedFile = Join-Path $env:TEMP 'ashanandanvan-fed.json'
[System.IO.File]::WriteAllText($fedFile, $fed)
az ad app federated-credential create --id $app.id --parameters $fedFile

Write-Host ''
Write-Host 'Created. GitHub secret AZURE_CLIENT_ID must be exactly:'
Write-Host $app.appId
Write-Host 'Re-open the secret, paste that value, save, then re-run the workflow.'
