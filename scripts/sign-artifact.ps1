param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [string]$CertificatePath = "artifacts/signing/cert.pfx",

    [string]$CertificatePassword = $env:SIGNING_CERTIFICATE_PASSWORD
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $FilePath)) {
    throw "Artifact not found: $FilePath"
}

if (-not (Test-Path $CertificatePath)) {
    Write-Host "No signing certificate at $CertificatePath — skipping signature for $FilePath"
    return
}

if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    throw "SIGNING_CERTIFICATE_PASSWORD is required when a certificate is present."
}

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $signtool) {
    throw "Windows SDK SignTool.exe not found."
}

Write-Host "Signing $FilePath..."
& $signtool.FullName sign `
    /fd SHA256 `
    /f $CertificatePath `
    /p $CertificatePassword `
    /tr http://timestamp.digicert.com `
    /td SHA256 `
    $FilePath

Write-Host "Signed: $FilePath"