param([switch]$Uninstall)
$ErrorActionPreference = 'Stop'
try {
 $name = 'Local.AudioConverter'
 if ($Uninstall) { Get-AppxPackage -Name $name | Remove-AppxPackage; Write-Host 'AudioConverter uninstalled. Signing certificate retained.'; exit 0 }
 if ([Environment]::OSVersion.Version.Build -lt 22000) { throw 'Windows 11 is required.' }
 $arch = $env:PROCESSOR_ARCHITEW6432
 if (!$arch) { $arch = $env:PROCESSOR_ARCHITECTURE }
 if ($arch -ne 'AMD64') { throw 'Intel/AMD x64 Windows is required.' }
 $info = Get-Content (Join-Path $PSScriptRoot 'package-info.json') -Raw | ConvertFrom-Json
 $package = Join-Path $PSScriptRoot 'AudioConverter.msix'
 $certificate = Join-Path $PSScriptRoot 'AudioConverter.cer'
 if ((Get-FileHash $package -Algorithm SHA256).Hash -ne $info.PackageSHA256) { throw 'Package checksum mismatch.' }
 if ((Get-FileHash $certificate -Algorithm SHA256).Hash -ne $info.CertificateSHA256) { throw 'Certificate checksum mismatch.' }
 $cert = [Security.Cryptography.X509Certificates.X509Certificate2]::new($certificate)
 if ($cert.Subject -ne 'CN=AudioConverter' -or $cert.Thumbprint -ne $info.CertificateThumbprint) { throw 'Unexpected certificate.' }
 if ($cert.NotAfter -lt (Get-Date) -or $cert.NotBefore -gt (Get-Date)) { throw 'Signing certificate is not within its validity period.' }
 if (!(Test-Path "Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)")) {
  Write-Host 'Administrator approval is required only to trust the AudioConverter signing certificate.'
  Write-Host "$($cert.Subject) / $($cert.Thumbprint)"
  $helper = Join-Path $PSScriptRoot 'Trust-Certificate.ps1'
  $params = '-NoProfile -ExecutionPolicy Bypass -File "' + $helper + '" -ExpectedHash ' + $info.CertificateSHA256
  $process = Start-Process "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList $params -Verb RunAs -Wait -PassThru -WindowStyle Hidden
  if ($process.ExitCode -ne 0) { throw 'Certificate registration failed or was cancelled.' }
 }
 $signature = Get-AuthenticodeSignature $package
 if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $cert.Thumbprint) { throw "Invalid package signature: $($signature.Status)" }
 if ((Get-AppxPackage -Name $name).IsDevelopmentMode) { throw 'Remove the development registration using install-modern.ps1 -Uninstall first.' }
 Add-AppxPackage -Path $package
 Get-AppxPackage -Name $name | Select-Object Name,Version,Status
 Write-Host 'Installed. Launch AudioConverter from Start or right-click WAV files. Sign out/in if the menu has not refreshed.'
 exit 0
} catch { Write-Error $_; exit 1 }
