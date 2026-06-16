Add-Type -AssemblyName System.Drawing

$pngPath = Resolve-Path 'Assets\icon.png'
$icoPath = Join-Path (Get-Location) 'Assets\icon.ico'

$png = [System.Drawing.Image]::FromFile($pngPath)
$bmp = New-Object System.Drawing.Bitmap($png)
$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)

$stream = [System.IO.File]::OpenWrite($icoPath)
$icon.Save($stream)
$stream.Close()

$icon.Dispose()
$bmp.Dispose()
$png.Dispose()

Write-Host "ICO created at: $icoPath"
