# Downloads and installs the Microsoft Edge WebView2 Evergreen Standalone Installer (x64).
# Run this only if ConsoleSplash reports that the WebView2 runtime is missing.

$ErrorActionPreference = 'Stop'

$url = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703'
$installer = Join-Path $env:TEMP 'MicrosoftEdgeWebview2Setup.exe'

Write-Host 'Downloading WebView2 runtime...'
Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing

Write-Host 'Installing WebView2 runtime...'
Start-Process -FilePath $installer -ArgumentList '/silent /install' -Wait

Write-Host 'Done. Start ConsoleSplash again.'
Write-Host 'Manual download: https://developer.microsoft.com/microsoft-edge/webview2/'
