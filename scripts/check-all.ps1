<#
.SYNOPSIS
    The quality gate run before declaring a phase done: format checks, analyzers, builds and all tests.
.DESCRIPTION
    Backend integration tests need SQL Server LocalDB (or set ETUTOR_TEST_DB to another server).
    The test database "ETutor_IntegrationTests" is dropped and recreated on every run.
#>
. "$PSScriptRoot/_common.ps1"

Push-Location $BackendRoot
try {
    Invoke-Step 'Backend: format check' {
        dotnet format ETutor.slnx --verify-no-changes --exclude src/ETutor.Infrastructure/Persistence/Migrations
    }
    Invoke-Step 'Backend: build (warnings are errors)' { dotnet build ETutor.slnx --nologo }
    Invoke-Step 'Backend: tests' { dotnet test ETutor.slnx --no-build --nologo }
}
finally { Pop-Location }

Push-Location $MobileRoot
try {
    Invoke-Step 'Mobile: packages and localizations' { flutter pub get }
    # Generated localization code (lib/l10n/generated) is excluded on purpose.
    Invoke-Step 'Mobile: format check' {
        dart format --output=none --set-exit-if-changed lib/app lib/core lib/design_system lib/features lib/main.dart test
    }
    Invoke-Step 'Mobile: analyze' { flutter analyze }
    Invoke-Step 'Mobile: tests' { flutter test }
}
finally { Pop-Location }

Write-Host "`nAll checks passed." -ForegroundColor Green
