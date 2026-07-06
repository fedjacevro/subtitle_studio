param(
    [string]$CertificateBase64 = $env:SIGNING_CERTIFICATE_BASE64,
    [string]$OutputPath = "artifacts/signing/cert.pfx"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($CertificateBase64)) {
    Write-Host "SIGNING_CERTIFICATE_BASE64 not set — release artifacts will remain unsigned."
    return
}

$dir = Split-Path $OutputPath -Parent
New-Item -ItemType Directory -Force -Path $dir | Out-Null
[IO.File]::WriteAllBytes($OutputPath, [Convert]::FromBase64String($CertificateBase64))
Write-Host "Signing certificate written to $OutputPath"