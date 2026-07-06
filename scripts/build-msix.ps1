param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/msix",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$layout = Join-Path $root "artifacts/msix-layout"
if (Test-Path $layout) {
    Remove-Item $layout -Recurse -Force
}

Write-Host "Publishing self-contained layout to $layout..."
dotnet publish SubtitleStudio.App/SubtitleStudio.App.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -o $layout

& (Join-Path $PSScriptRoot "generate-msix-assets.ps1")

$manifestSource = Join-Path $root "SubtitleStudio.App/Package.appxmanifest"
$manifestTarget = Join-Path $layout "AppxManifest.xml"
Copy-Item $manifestSource $manifestTarget

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $parts = $Version.TrimStart('v').Split('.')
    while ($parts.Count -lt 4) { $parts += '0' }
    $manifestVersion = ($parts[0..3] -join '.')
    $content = Get-Content $manifestTarget -Raw
    $content = $content -replace 'Version="[^"]+"', "Version=`"$manifestVersion`""
    Set-Content -Path $manifestTarget -Value $content -NoNewline
    Write-Host "MSIX manifest version set to $manifestVersion"
}

Copy-Item -Recurse (Join-Path $root "SubtitleStudio.App/Assets") (Join-Path $layout "Assets")

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $makeappx) {
    throw "Windows SDK MakeAppx.exe not found. Install the Windows 10/11 SDK."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$msixPath = Join-Path $OutputDir "SubtitleStudio.msix"
if (Test-Path $msixPath) {
    Remove-Item $msixPath -Force
}

Write-Host "Packing MSIX with $($makeappx.FullName)..."
& $makeappx.FullName pack /d $layout /p $msixPath /o

Write-Host "MSIX created: $msixPath"