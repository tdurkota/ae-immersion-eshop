#!/usr/bin/env pwsh

################################################################################
# GitHub Actions Secrets Setup (PowerShell)
#
# Reads secrets from .env.local and sets them in GitHub
#
# Usage: .\scripts\setup-github-secrets.ps1
#
################################################################################

$ErrorActionPreference = "Stop"

# Colors
function Write-Error-Red($message) { Write-Host $message -ForegroundColor Red }
function Write-Success-Green($message) { Write-Host $message -ForegroundColor Green }
function Write-Warning-Yellow($message) { Write-Host $message -ForegroundColor Yellow }
function Write-Info-Blue($message) { Write-Host $message -ForegroundColor Blue }

Write-Info-Blue "🔧 GitHub Actions Secrets Setup"
Write-Host "================================="
Write-Host ""

# Check if .env.local exists
if (-not (Test-Path .env.local)) {
    Write-Error-Red "❌ Error: .env.local not found!"
    Write-Host ""
    Write-Host "Please create .env.local first with your Azure and other credentials."
    Write-Host "See docs/AZURE_SETUP.md for instructions."
    exit 1
}

# Check if gh CLI is installed
try {
    $ghVersion = gh --version 2>$null
} catch {
    Write-Error-Red "❌ Error: GitHub CLI (gh) is not installed!"
    Write-Host ""
    Write-Host "Install it from: https://cli.github.com/"
    exit 1
}

# Check if authenticated with GitHub
$ghStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Red "❌ Error: Not authenticated with GitHub!"
    Write-Host ""
    Write-Host "Run: gh auth login"
    exit 1
}

# Load secrets from .env.local
Write-Warning-Yellow "📂 Loading secrets from .env.local..."
$envVars = @{}

Get-Content .env.local | ForEach-Object {
    if ($_ -match "^([^=]+)=(.*)$") {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim().TrimStart("'").TrimEnd("'").TrimStart('"').TrimEnd('"')
        
        # Skip comments and empty lines
        if ($key -and -not $key.StartsWith("#")) {
            $envVars[$key] = $value
        }
    }
}

Write-Host ""

# Function to set secret safely
function Set-GitHubSecret {
    param(
        [string]$Key,
        [string]$Value
    )
    
    if ([string]::IsNullOrEmpty($Value)) {
        Write-Warning-Yellow "⚠️  Skipping $Key (not set in .env.local)"
        return
    }
    
    $Value | gh secret set $Key 2>$null
    Write-Success-Green "✅ Set $Key"
}

# Set Azure Secrets
Write-Warning-Yellow "📦 Setting Azure secrets..."
Set-GitHubSecret "AZURE_CREDENTIALS" $envVars['AZURE_CREDENTIALS']
Set-GitHubSecret "AZURE_SUBSCRIPTION_ID" $envVars['AZURE_SUBSCRIPTION_ID']
Set-GitHubSecret "AZURE_RESOURCE_GROUP" $envVars['AZURE_RESOURCE_GROUP']
Set-GitHubSecret "AZURE_LOCATION" $envVars['AZURE_LOCATION']

Write-Host ""
Write-Warning-Yellow "🐳 Setting ACR secrets..."
Set-GitHubSecret "ACR_REGISTRY_NAME" $envVars['ACR_REGISTRY_NAME']
Set-GitHubSecret "ACR_USERNAME" $envVars['ACR_USERNAME']
Set-GitHubSecret "ACR_PASSWORD" $envVars['ACR_PASSWORD']

Write-Host ""
Write-Warning-Yellow "🤖 Setting Foundry (Chatbot) secrets..."
Set-GitHubSecret "FOUNDRY_ENDPOINT" $envVars['FOUNDRY_ENDPOINT']
Set-GitHubSecret "FOUNDRY_KEY" $envVars['FOUNDRY_KEY']

Write-Host ""
Write-Warning-Yellow "📋 Setting Linear secrets..."
Set-GitHubSecret "LINEAR_API_KEY" $envVars['LINEAR_API_KEY']
Set-GitHubSecret "LINEAR_API_ENDPOINT" $envVars['LINEAR_API_ENDPOINT']
Set-GitHubSecret "LINEAR_BUILD_ISSUE_ID" $envVars['LINEAR_BUILD_ISSUE_ID']

Write-Host ""
Write-Warning-Yellow "🎛️  Setting Aspire secrets..."
Set-GitHubSecret "ASPIRE_MODULE_PATH" $envVars['ASPIRE_MODULE_PATH']

Write-Host ""
Write-Success-Green "═════════════════════════════════"
Write-Success-Green "✅ All secrets configured!"
Write-Success-Green "═════════════════════════════════"
Write-Host ""
Write-Warning-Yellow "📋 Verifying secrets (names only):"
gh secret list

Write-Host ""
Write-Success-Green "🎉 Setup complete!"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Verify secrets are correct in GitHub Settings"
Write-Host "2. Push your feature branch: git push origin feature/github-actions-cicd"
Write-Host "3. Create a pull request"
Write-Host "4. GitHub Actions will automatically test the configuration"
Write-Host ""
Write-Host "For more info, see: docs/AZURE_SETUP.md and docs/GITHUB_ACTIONS_SECRETS.md"
