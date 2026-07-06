$ErrorActionPreference = 'Stop'

$assetsDir = Join-Path $PSScriptRoot '..\SubtitleStudio.App\Assets'
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

Add-Type -AssemblyName System.Drawing

function New-LogoPng {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Background,
        [System.Drawing.Color]$Foreground
    )

    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear($Background)

    $accent = New-Object System.Drawing.SolidBrush $Foreground
    $accentWidth = [Math]::Max(4, [int]($Width * 0.18))
    $graphics.FillRectangle($accent, 0, 0, $accentWidth, $Height)

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)

    $graphics.Dispose()
    $bitmap.Dispose()
    $accent.Dispose()
}

$bg = [System.Drawing.Color]::FromArgb(255, 30, 30, 46)
$fg = [System.Drawing.Color]::FromArgb(255, 137, 180, 250)

New-LogoPng -Path (Join-Path $assetsDir 'StoreLogo.png') -Width 50 -Height 50 -Background $bg -Foreground $fg
New-LogoPng -Path (Join-Path $assetsDir 'Square44x44Logo.png') -Width 44 -Height 44 -Background $bg -Foreground $fg
New-LogoPng -Path (Join-Path $assetsDir 'Square150x150Logo.png') -Width 150 -Height 150 -Background $bg -Foreground $fg
New-LogoPng -Path (Join-Path $assetsDir 'Wide310x150Logo.png') -Width 310 -Height 150 -Background $bg -Foreground $fg

Write-Host "MSIX assets generated in $assetsDir"