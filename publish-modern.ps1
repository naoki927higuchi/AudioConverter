param([string]$Version = '2.0.0')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build-native.cmd')
if ($LASTEXITCODE) { throw 'Native build failed.' }
$dist = Join-Path $PSScriptRoot 'dist'
dotnet publish (Join-Path $PSScriptRoot 'AudioConverter.csproj') -c Release -r win-x64 --self-contained true -p:Version=$Version -o $dist
if ($LASTEXITCODE) { throw '.NET publish failed.' }
Copy-Item (Join-Path $PSScriptRoot 'native\AudioConverterCommand.dll') $dist -Force
Copy-Item (Join-Path $PSScriptRoot 'AppxManifest.xml') $dist -Force
Copy-Item (Join-Path $PSScriptRoot 'Assets') $dist -Recurse -Force

