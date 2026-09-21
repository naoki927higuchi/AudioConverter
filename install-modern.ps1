param([switch]$Uninstall)
$ErrorActionPreference = 'Stop'
if ($Uninstall) { Get-AppxPackage -Name Local.AudioConverter | Remove-AppxPackage; exit }
if (Get-AppxPackage -Name Local.AudioConverter) { throw 'AudioConverter is already registered. Remove it before development registration.' }
Add-AppxPackage -Register (Join-Path $PSScriptRoot 'dist\AppxManifest.xml')
Get-AppxPackage -Name Local.AudioConverter | Select-Object Name,Status,InstallLocation
