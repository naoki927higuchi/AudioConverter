param([string]$Version = '1.0.1')
$ErrorActionPreference = 'Stop'
foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'distribution') -Filter '*.ps1')) {
 $tokens = $null; $errors = $null
 [System.Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors) | Out-Null
 if ($errors) { throw ($errors | Out-String) }
 Write-Host "$($file.Name): syntax OK"
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Security
$folder = Join-Path $PSScriptRoot "release\AudioConverter-$Version-x64"
$info = Get-Content -LiteralPath (Join-Path $folder 'package-info.json') -Raw | ConvertFrom-Json
$package = Join-Path $folder 'AudioConverter.msix'
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $info.PackageSHA256) { throw 'Package hash mismatch' }
$certificate = Join-Path $folder 'AudioConverter.cer'
if ((Get-FileHash $certificate -Algorithm SHA256).Hash -ne $info.CertificateSHA256) { throw 'Certificate hash mismatch' }
$cert = [Security.Cryptography.X509Certificates.X509Certificate2]::new($certificate)
if ($cert.HasPrivateKey -or $cert.Thumbprint -ne $info.CertificateThumbprint) { throw 'Invalid public certificate' }
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
 foreach ($required in @('AudioConverter.exe','AudioConverterCommand.dll','Assets/Logo.png','ffmpeg/ffmpeg.exe','ffmpeg/LICENSE.txt','ffmpeg/NOTICE.txt','ffmpeg/build-info.json','ffmpeg/UPSTREAM-README.txt')) {
  if (!$zip.GetEntry($required)) { throw "Required package entry missing: $required" }
 }
 $binaryStream = $zip.GetEntry('ffmpeg/ffmpeg.exe').Open()
 $binaryHasher = [Security.Cryptography.SHA256]::Create()
 try { $binaryHash = [BitConverter]::ToString($binaryHasher.ComputeHash($binaryStream)).Replace('-','') } finally { $binaryStream.Dispose(); $binaryHasher.Dispose() }
 if ($binaryHash -ne $info.FFmpeg.SHA256) { throw 'Packaged FFmpeg hash mismatch' }
 $manifestReader = [IO.StreamReader]::new($zip.GetEntry('AppxManifest.xml').Open())
 try { [xml]$manifest = $manifestReader.ReadToEnd() } finally { $manifestReader.Dispose() }
 if ($manifest.Package.Identity.Name -ne 'Local.AudioConverter' -or $manifest.Package.Identity.Version -ne $info.Version) { throw 'Package identity mismatch' }
 $entry = $zip.GetEntry('AppxSignature.p7x')
 $stream = $entry.Open(); $memory = New-Object IO.MemoryStream
 try { $stream.CopyTo($memory); $bytes = $memory.ToArray() } finally { $stream.Dispose(); $memory.Dispose() }
 $cms = New-Object System.Security.Cryptography.Pkcs.SignedCms
 $cms.Decode([byte[]]$bytes[4..($bytes.Length-1)])
 $cms.CheckSignature($true)
 if ($cms.SignerInfos[0].Certificate.Thumbprint -ne $info.CertificateThumbprint) { throw 'Signer mismatch' }
 Write-Host 'Cryptographic signature verified (without installing certificate trust).'
 $reader = New-Object IO.StreamReader($zip.GetEntry('AppxBlockMap.xml').Open())
 try { [xml]$map = $reader.ReadToEnd() } finally { $reader.Dispose() }
 $sha = [Security.Cryptography.SHA256]::Create()
 try {
  foreach ($file in $map.BlockMap.File) {
   $entry = $zip.GetEntry($file.Name.Replace('\','/'))
   if (!$entry) { throw "Missing entry: $($file.Name)" }
   $stream = $entry.Open()
   try {
    $buffer = New-Object byte[] 65536
    foreach ($block in $file.Block) {
     $count=0
     while ($count -lt $buffer.Length) { $read=$stream.Read($buffer,$count,$buffer.Length-$count); if (!$read) { break }; $count += $read }
     if ([Convert]::ToBase64String($sha.ComputeHash($buffer,0,$count)) -ne $block.Hash) { throw "Block hash mismatch: $($file.Name)" }
    }
    if ($stream.ReadByte() -ne -1) { throw 'Unexpected trailing data' }
   } finally { $stream.Dispose() }
  }
 } finally { $sha.Dispose() }
 Write-Host 'All package payload block hashes verified.'
 if ($zip.Entries.FullName -match '\.(obj|pdb|lib|exp|pfx)$') { throw 'Unexpected build/private file in package' }
} finally { $zip.Dispose() }
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $PSScriptRoot "release\AudioConverter-$Version-x64.zip"))
try {
 $archive.Entries | Select-Object FullName,Length
 if ($archive.Entries.FullName -match '\.(pfx|key)$') { throw 'Private key in ZIP' }
 $archiveHasher = [Security.Cryptography.SHA256]::Create()
 try {
  foreach ($file in Get-ChildItem $folder -File) {
   $entry = $archive.Entries | Where-Object { $_.FullName.Replace('\','/') -eq "AudioConverter-$Version-x64/$($file.Name)" }
   if (!$entry) { throw "Missing release ZIP entry: $($file.Name)" }
   $stream = $entry.Open()
   try { $hash = [BitConverter]::ToString($archiveHasher.ComputeHash($stream)).Replace('-','') } finally { $stream.Dispose() }
   if ($hash -ne (Get-FileHash $file.FullName -Algorithm SHA256).Hash) { throw "ZIP content mismatch: $($file.Name)" }
  }
 } finally { $archiveHasher.Dispose() }
} finally { $archive.Dispose() }
Write-Host 'Release validation passed.'

