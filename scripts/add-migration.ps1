<#
.SYNOPSIS
    Creates a new EF Core migration after you change entities or configurations.
.EXAMPLE
    ./scripts/add-migration.ps1 -Name AddEnrollments
#>
param([Parameter(Mandatory)] [ValidatePattern('^[A-Z][A-Za-z0-9]+$')] [string] $Name)
. "$PSScriptRoot/_common.ps1"
Invoke-Step "Adding migration $Name" {
    Invoke-Ef @('migrations', 'add', $Name, '--output-dir', 'Persistence/Migrations')
}
Write-Host 'Review the generated migration before applying it (./scripts/db-update.ps1).' -ForegroundColor Yellow
