<#
.SYNOPSIS
    Applies all pending EF Core migrations (schema + reference data) to the configured database.
.DESCRIPTION
    Uses ConnectionStrings:Database from appsettings.Development.json unless the environment
    variable ConnectionStrings__Database is set.
#>
. "$PSScriptRoot/_common.ps1"
Invoke-Step 'Applying migrations' { Invoke-Ef @('database', 'update') }
