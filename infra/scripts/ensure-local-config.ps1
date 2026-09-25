<#
.SYNOPSIS
  Creates local .env and secrets.json from examples when they are missing.
#>
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$web = Join-Path $root "src\AshaNandanvan.Web"

$envExample = Join-Path $root ".env.example"
$envFile = Join-Path $root ".env"
if (-not (Test-Path $envFile)) {
    Copy-Item $envExample $envFile
    Write-Host "Created $envFile from .env.example"
}

$secretsExample = Join-Path $web "secrets.json.example"
$secretsFile = Join-Path $web "secrets.json"
if (-not (Test-Path $secretsFile)) {
    Copy-Item $secretsExample $secretsFile
    Write-Host "Created $secretsFile from secrets.json.example. Add Google, Square, and admin values there."
}
