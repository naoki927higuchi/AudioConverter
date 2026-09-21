$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assets = New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'Assets')
$images = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
 $bitmap = [Drawing.Bitmap]::new($size,$size)
 $g = [Drawing.Graphics]::FromImage($bitmap)
 $background = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(26,83,119))
 $pen = [Drawing.Pen]::new([Drawing.Color]::White,[single]($size / 24))
 $font = [Drawing.Font]::new('Segoe UI',[single]($size * 0.30),[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
 $format = [Drawing.StringFormat]::new()
 $stream = [IO.MemoryStream]::new()
 try {
  $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
  $g.Clear([Drawing.Color]::Transparent)
  $g.FillRectangle($background,0,0,$size,$size)
  $margin = [single]($size * 0.10)
  $g.DrawRectangle($pen,$margin,$margin,[single]($size-$margin*2),[single]($size-$margin*2))
  $format.Alignment = 'Center'; $format.LineAlignment = 'Center'
  $g.DrawString('mp3',$font,[Drawing.Brushes]::White,[Drawing.RectangleF]::new(0,0,$size,$size),$format)
  $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
  $images += ,@($size,$stream.ToArray())
  if ($size -eq 256) { $bitmap.Save((Join-Path $assets.FullName 'Logo.png'),[Drawing.Imaging.ImageFormat]::Png) }
 } finally { $stream.Dispose(); $format.Dispose(); $font.Dispose(); $pen.Dispose(); $background.Dispose(); $g.Dispose(); $bitmap.Dispose() }
}
$file = [IO.File]::Create((Join-Path $assets.FullName 'AudioConverter.ico'))
$writer = [IO.BinaryWriter]::new($file)
try {
 $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
 $offset = 6 + 16 * $images.Count
 foreach ($entry in $images) {
  $dimension = if ($entry[0] -eq 256) { 0 } else { $entry[0] }
  $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
  $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$entry[1].Length); $writer.Write([uint32]$offset)
  $offset += $entry[1].Length
 }
 foreach ($entry in $images) { $writer.Write([byte[]]$entry[1]) }
} finally { $writer.Dispose(); $file.Dispose() }
