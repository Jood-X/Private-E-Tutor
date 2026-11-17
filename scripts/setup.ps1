<#
.SYNOPSIS
    One-time setup for a new developer: checks tools, restores dependencies and creates the local database.
.EXAMPLE
    ./scripts/setup.ps1
#>
. "$PSScriptRoot/_common.ps1"

Invoke-Step 'Checking required tools' {
    foreach ($tool in 'dotnet', 'flutter', 'git') {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "'$tool' was not found on PATH. See docs/setup.md (Prerequisites)."
        }
    }
    $sdks = dotnet --list-sdks
    if (-not ($sdks -match '^10\.')) {
        throw '.NET 10 SDK is required. See docs/setup.md.'
    }
}

Invoke-Step 'Restoring backend packages and tools' {
    Push-Location $BackendRoot
    try { dotnet tool restore; dotnet restore } finally { Pop-Location }
}

Invoke-Step 'Creating/updating the local database (migrations + reference data)' {
    Invoke-Ef @('database', 'update')
}

Invoke-Step 'Fetching Flutter packages and generating localizations' {
    Push-Location $MobileRoot
    try { flutter pub get } finally { Pop-Location }
}

Write-Host "`nSetup complete. Next: ./scripts/run-api.ps1, then run the app (docs/setup.md)." -ForegroundColor Green
