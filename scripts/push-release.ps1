param(
    [string]$Tag = "v1.0.0",
    [string]$Repo = "sisquo76/subtitle_studio",
    [ValidateSet("public", "private")]
    [string]$Visibility = "public"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) is required. Install with: winget install GitHub.cli"
}

$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "GitHub CLI is not authenticated. Starting login..."
    gh auth login --hostname github.com --git-protocol https --web
}

$remotes = git remote
if (-not ($remotes -contains "origin")) {
    Write-Host "Creating GitHub repository $Repo and setting origin..."
    gh repo create $Repo --$Visibility --source=. --remote=origin --description "CPU-only subtitle transcription, translation, and export tool"
} else {
    Write-Host "Pushing to existing origin remote..."
}

Write-Host "Pushing branch and tag $Tag..."
git push -u origin HEAD
git push origin $Tag

Write-Host ""
Write-Host "Release triggered. Monitor progress at:"
Write-Host "  https://github.com/$Repo/actions"
Write-Host "  https://github.com/$Repo/releases/tag/$Tag"