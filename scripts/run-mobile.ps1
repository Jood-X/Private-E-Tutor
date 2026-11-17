<#
.SYNOPSIS
    Runs the Flutter app from anywhere in the repository (Flutter itself must run inside mobile/).
.PARAMETER Web
    Runs in Chrome on port 8686 (the origin the development API allows) instead of the Android emulator.
.EXAMPLE
    ./scripts/run-mobile.ps1          # Android emulator / connected device
.EXAMPLE
    ./scripts/run-mobile.ps1 -Web     # Chrome
#>
param([switch] $Web)
. "$PSScriptRoot/_common.ps1"

Push-Location $MobileRoot
try {
    if ($Web) {
        flutter run -d chrome --web-port 8686 --dart-define=API_BASE_URL=http://localhost:5258
    }
    else {
        flutter run
    }
}
finally {
    Pop-Location
}
