<#
.SYNOPSIS
    DESTRUCTIVE: drops the local development database and recreates it from the migrations.
.PARAMETER Force
    Skips the confirmation prompt.
#>
param([switch] $Force)
. "$PSScriptRoot/_common.ps1"

if (-not $Force) {
    $answer = Read-Host 'This permanently deletes ALL data in the local development database. Type "reset" to continue'
    if ($answer -ne 'reset') {
        Write-Host 'Cancelled.'
        exit 1
    }
}

Invoke-Step 'Dropping the database' { Invoke-Ef @('database', 'drop', '--force') }
Invoke-Step 'Recreating it from migrations' { Invoke-Ef @('database', 'update') }
