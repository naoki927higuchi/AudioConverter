param([ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version = '1.0.1')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'publish-modern.ps1') -Version $Version
if ($LASTEXITCODE) { throw 'Publish failed.' }
$sdk = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'x64\makeappx.exe') } | Sort-Object Name -Descending | Select-Object -First 1
if (!$sdk) { throw 'Windows SDK not found.' }
$sdk = Join-Path $sdk.FullName 'x64'
$payload = New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot ('artifacts\payload-' + [guid]::NewGuid().ToString('N')))
$out = New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot "release\AudioConverter-$Version-x64") -Force
foreach ($file in Get-ChildItem (Join-Path $PSScriptRoot 'dist') -File | Where-Object Extension -in '.exe','.dll','.json') { Copy-Item $file.FullName $payload.FullName }
foreach ($folder in @('Assets','ffmpeg')) { Copy-Item (Join-Path $PSScriptRoot "dist\$folder") $payload.FullName -Recurse }
[xml]$manifest = Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw
$manifest.Package.Identity.Version = "$Version.0"
$manifest.Save((Join-Path $payload.FullName 'AppxManifest.xml'))
$package = Join-Path $out.FullName 'AudioConverter.msix'
& "$sdk\makeappx.exe" pack /d $payload.FullName /p $package /o
if ($LASTEXITCODE) { throw 'Package creation failed.' }
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq 'CN=AudioConverter' -and $_.FriendlyName -eq 'AudioConverter local distribution' -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddMonths(1) } | Sort-Object NotAfter -Descending | Select-Object -First 1
if (!$cert) { $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=AudioConverter' -FriendlyName 'AudioConverter local distribution' -CertStoreLocation Cert:\CurrentUser\My -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(5) }
$cer = Join-Path $out.FullName 'AudioConverter.cer'
Export-Certificate -Cert $cert -FilePath $cer -Force | Out-Null
& "$sdk\signtool.exe" sign /fd SHA256 /s My /sha1 $cert.Thumbprint $package
if ($LASTEXITCODE) { throw 'Signing failed.' }
Copy-Item (Join-Path $PSScriptRoot 'distribution\*') $out.FullName -Force
$ffmpeg = Get-Content (Join-Path $PSScriptRoot 'vendor\ffmpeg\build-info.json') -Raw | ConvertFrom-Json
[ordered]@{
 Version = "$Version.0"; Architecture = 'x64'
 PackageSHA256 = (Get-FileHash $package -Algorithm SHA256).Hash
 CertificateSHA256 = (Get-FileHash $cer -Algorithm SHA256).Hash
 CertificateThumbprint = $cert.Thumbprint; CertificateExpires = $cert.NotAfter.ToString('o')
 FFmpeg = $ffmpeg
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $out.FullName 'package-info.json') -Encoding UTF8
Copy-Item (Join-Path $PSScriptRoot 'vendor\ffmpeg\LICENSE.txt') (Join-Path $out.FullName 'FFmpeg-LICENSE.txt') -Force
Copy-Item (Join-Path $PSScriptRoot 'vendor\ffmpeg\NOTICE.txt') (Join-Path $out.FullName 'FFmpeg-NOTICE.txt') -Force
$zip = Join-Path $PSScriptRoot "release\AudioConverter-$Version-x64.zip"
Compress-Archive -Path $out.FullName -DestinationPath $zip -Force
Get-Item $zip | Select-Object FullName,Length
