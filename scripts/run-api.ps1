<#
.SYNOPSIS
    Runs the API locally on http://localhost:5258 (Swagger UI at /swagger).
#>
. "$PSScriptRoot/_common.ps1"
dotnet run --project $ApiProject --launch-profile http
