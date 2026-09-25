<#
.SYNOPSIS
  Loads SQL settings from .env into the current session for host debugging.

.EXAMPLE
  . .\infra\scripts\export-docker-env.ps1
  dotnet run --project src/AshaNandanvan.Web --launch-profile "https (Docker SQL)"
#>
[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) ".env")
)

$EnvFile = [System.IO.Path]::GetFullPath($EnvFile)
if (-not (Test-Path $EnvFile)) {
    $example = Join-Path (Split-Path $EnvFile) ".env.example"
    throw ".env not found at $EnvFile. Copy .env.example to .env (see $example)."
}

$sqlKeys = @(
    "MSSQL_SA_PASSWORD",
    "SQL_SERVER",
    "SQL_DATABASE",
    "SQL_USER",
    "SQL_ENCRYPT",
    "SQL_TRUST_SERVER_CERTIFICATE",
    "SQL_MULTIPLE_ACTIVE_RESULT_SETS",
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED"
)

Get-Content $EnvFile | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith("#")) {
        return
    }

    $parts = $line.Split("=", 2)
    if ($parts.Length -ne 2) {
        return
    }

    $name = $parts[0].Trim()
    if ($sqlKeys -notcontains $name) {
        return
    }

    $value = $parts[1].Trim().Trim('"').Trim("'")
    Set-Item -Path "Env:$name" -Value $value
}

$env:ASHA_SQL_SOURCE = "docker"
Write-Host "Loaded SQL settings from $EnvFile"
Write-Host "ASHA_SQL_SOURCE=docker — Google / Square / admin still come from secrets.json"
