# Shared helpers for the development scripts. Dot-source this file; do not run it directly.
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$BackendRoot = Join-Path $RepoRoot 'backend'
$MobileRoot = Join-Path $RepoRoot 'mobile'
$ApiProject = Join-Path $BackendRoot 'src/ETutor.Api'
$InfrastructureProject = Join-Path $BackendRoot 'src/ETutor.Infrastructure'

function Invoke-Step([string] $Title, [scriptblock] $Action) {
    Write-Host "==> $Title" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Step failed: $Title (exit code $LASTEXITCODE)"
    }
}

function Invoke-Ef([string[]] $Arguments) {
    Push-Location $BackendRoot
    try {
        $env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { 'Development' }
        dotnet tool restore | Out-Null
        dotnet ef @Arguments --project $InfrastructureProject --startup-project $ApiProject
    }
    finally {
        Pop-Location
    }
}
