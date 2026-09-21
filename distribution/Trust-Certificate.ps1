param([Parameter(Mandatory=$true)][string]$ExpectedHash)
$ErrorActionPreference = 'Stop'
try {
 $file = Join-Path $PSScriptRoot 'AudioConverter.cer'
 if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $ExpectedHash) { throw 'Certificate checksum mismatch.' }
 $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($file)
 if ($cert.Subject -ne 'CN=AudioConverter') { throw 'Unexpected certificate subject.' }
 Import-Certificate -FilePath $file -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
 exit 0
} catch { Write-Error $_; exit 1 }

