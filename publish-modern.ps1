param([string]$Version = '1.0.1')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build-native.cmd')
if ($LASTEXITCODE) { throw 'Native build failed.' }
$dist = Join-Path $PSScriptRoot 'dist'
dotnet publish (Join-Path $PSScriptRoot 'AudioConverter.csproj') -c Release -r win-x64 --self-contained true -p:Version=$Version -o $dist
if ($LASTEXITCODE) { throw '.NET publish failed.' }
Copy-Item (Join-Path $PSScriptRoot 'native\AudioConverterCommand.dll') $dist -Force
Copy-Item (Join-Path $PSScriptRoot 'AppxManifest.xml') $dist -Force
if (!(Test-Path (Join-Path $PSScriptRoot 'vendor\ffmpeg\ffmpeg.exe'))) { throw 'Run prepare-ffmpeg.ps1 first.' }
$ffmpegInfo = Get-Content (Join-Path $PSScriptRoot 'vendor\ffmpeg\build-info.json') -Raw | ConvertFrom-Json
if ((Get-FileHash (Join-Path $PSScriptRoot 'vendor\ffmpeg\ffmpeg.exe') -Algorithm SHA256).Hash -ne $ffmpegInfo.SHA256) { throw 'Bundled FFmpeg does not match the reviewed SHA256.' }
Copy-Item (Join-Path $PSScriptRoot 'vendor\ffmpeg') $dist -Recurse -Force
Copy-Item (Join-Path $PSScriptRoot 'Assets') $dist -Recurse -Force

