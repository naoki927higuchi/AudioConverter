param([Parameter(Mandatory=$true)][string]$FFmpegRoot)
$ErrorActionPreference = 'Stop'
# Point to the extracted upstream 9.0.1-full_build directory, not its bin directory.
$destination = Join-Path $PSScriptRoot 'vendor\ffmpeg'
$info = Get-Content (Join-Path $destination 'build-info.json') -Raw | ConvertFrom-Json
$binary = Join-Path $FFmpegRoot 'bin\ffmpeg.exe'
if ((Get-FileHash $binary -Algorithm SHA256).Hash -ne $info.SHA256) { throw 'FFmpeg hash differs from the reviewed build. Review its license and update build-info.json before upgrading.' }
foreach ($file in @('LICENSE','README.txt')) { if (!(Test-Path (Join-Path $FFmpegRoot $file))) { throw "Missing upstream $file" } }
Copy-Item $binary $destination -Force
Copy-Item (Join-Path $FFmpegRoot 'LICENSE') (Join-Path $destination 'LICENSE.txt') -Force
Copy-Item (Join-Path $FFmpegRoot 'README.txt') (Join-Path $destination 'UPSTREAM-README.txt') -Force
Write-Host 'Reviewed FFmpeg executable and upstream license prepared.'
