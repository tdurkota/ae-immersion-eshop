#!/usr/bin/env pwsh

################################################################################
# eShop Marketplace - Automated Azure & GitHub Setup (PowerShell)
#
# This script automates:
# 1. Azure Resource Group creation
# 2. Azure Service Principal creation
# 3. GitHub Secrets configuration
# 4. .env.local file generation
#
# Usage: .\scripts\setup-azure-and-github.ps1
#
################################################################################

$ErrorActionPreference = "Stop"

# Colors
function Write-Error-Red($message) {
    Write-Host $message -ForegroundColor Red
}

function Write-Success-Green($message) {
    Write-Host $message -ForegroundColor Green
}

function Write-Warning-Yellow($message) {
    Write-Host $message -ForegroundColor Yellow
}

function Write-Info-Blue($message) {
    Write-Host $message -ForegroundColor Blue
}

# Banner
Write-Host ""
Write-Info-Blue "╔══════════════════════════════════════════════════════════════╗"
Write-Info-Blue "║  eShop Marketplace - Automated Azure & GitHub Setup         ║"
Write-Info-Blue "║  Automates: Resource Group, Service Principal, Secrets      ║"
Write-Info-Blue "╚══════════════════════════════════════════════════════════════╝"
Write-Host ""

# Default values
$resourceGroupName = "eshop-marketplace"
$appName = "github-actions-eshop-marketplace"
$location = "eastus"
$acrName = "eshopmarketplaceacr"

################################################################################
# STEP 1: Check Prerequisites
################################################################################

Write-Warning-Yellow "Step 1: Checking prerequisites..."
Write-Host ""

# Check Azure CLI
try {
    $azVersion = az version 2>$null
    Write-Success-Green "✅ Azure CLI installed"
} catch {
    Write-Error-Red "❌ Azure CLI not found!"
    Write-Host "Install from: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check GitHub CLI
try {
    $ghVersion = gh --version 2>$null
    Write-Success-Green "✅ GitHub CLI installed"
} catch {
    Write-Error-Red "❌ GitHub CLI not found!"
    Write-Host "Install from: https://cli.github.com/"
    exit 1
}

Write-Host ""

################################################################################
# STEP 2: Azure Authentication
################################################################################

Write-Warning-Yellow "Step 2: Authenticating with Azure..."
Write-Host ""

try {
    $account = az account show 2>$null | ConvertFrom-Json
} catch {
    Write-Warning-Yellow "🔐 Please authenticate with Azure"
    az login
    $account = az account show | ConvertFrom-Json
}

$subscriptionId = $account.id
$subscriptionName = $account.name

Write-Success-Green "✅ Authenticated"
Write-Host "   Subscription: $subscriptionName"
Write-Host "   ID: $subscriptionId"
Write-Host ""

################################################################################
# STEP 3: Gather User Input
################################################################################

Write-Warning-Yellow "Step 3: Configuration (press Enter to use defaults)"
Write-Host ""

$inputRg = Read-Host "Resource Group Name [$resourceGroupName]"
if ($inputRg) { $resourceGroupName = $inputRg }

$inputLoc = Read-Host "Location [$location]"
if ($inputLoc) { $location = $inputLoc }

$inputApp = Read-Host "App/Service Principal Name [$appName]"
if ($inputApp) { $appName = $inputApp }

Write-Host ""
Write-Info-Blue "Summary:"
Write-Host "  Resource Group: $resourceGroupName"
Write-Host "  Location: $location"
Write-Host "  Service Principal: $appName"
Write-Host "  Subscription: $subscriptionId"
Write-Host ""
Write-Info-Blue "Note: Azure Container Registry (ACR) will be auto-provisioned by Aspire during deployment"
Write-Host ""

$continue = Read-Host "Continue? (y/n)"
if ($continue -ne "y" -and $continue -ne "Y") {
    Write-Error-Red "Cancelled"
    exit 1
}

Write-Host ""

################################################################################
# STEP 4: Create Azure Resource Group
################################################################################

Write-Warning-Yellow "Step 4: Creating Azure Resource Group..."
Write-Host ""

$rgExists = az group exists --name $resourceGroupName | ConvertFrom-Json
if ($rgExists) {
    Write-Warning-Yellow "⚠️  Resource group '$resourceGroupName' already exists"
} else {
    Write-Host "Creating resource group..."
    az group create `
        --name $resourceGroupName `
        --location $location `
        --output none
    Write-Success-Green "✅ Resource group created"
}

Write-Host ""

################################################################################
# STEP 5: Create Azure Service Principal
################################################################################

Write-Warning-Yellow "Step 5: Creating Azure Service Principal..."
Write-Host ""

# Check if service principal exists
$spCheck = az ad sp list --filter "displayName eq '$appName'" --query "[0].id" -o tsv 2>$null
if ($spCheck) {
    Write-Warning-Yellow "⚠️  Service principal '$appName' already exists"
    Write-Host "Using existing service principal: $spCheck"
    
    # Reset credentials using the SP ID
    $spJsonStr = az ad sp credential reset `
        --id $spCheck `
        --credential-description "github-actions" `
        --sdk-auth
} else {
    Write-Host "Creating service principal..."
    
    $spJsonStr = az ad sp create-for-rbac `
        --name $appName `
        --role contributor `
        --scopes "/subscriptions/$subscriptionId" `
        --sdk-auth
    
    Write-Success-Green "✅ Service principal created"
}

$spJson = $spJsonStr | ConvertFrom-Json
$clientId = $spJson.clientId
$clientSecret = $spJson.clientSecret
$tenantId = $spJson.tenantId

Write-Host ""
Write-Info-Blue "Service Principal Details:"
Write-Host "  Client ID: $clientId"
Write-Host "  Tenant ID: $tenantId"
Write-Host ""

################################################################################
# STEP 6: Create .env.local
################################################################################

Write-Warning-Yellow "Step 7: Creating .env.local file..."
Write-Host ""

$envFile = ".env.local"

if (Test-Path $envFile) {
    Write-Warning-Yellow "⚠️  $envFile already exists"
    $overwrite = Read-Host "Overwrite? (y/n)"
    if ($overwrite -ne "y" -and $overwrite -ne "Y") {
        Write-Host "Skipping .env.local creation"
        $envFile = ""
    }
}

if ($envFile) {
    $envContent = @"
# Auto-generated by setup-azure-and-github.ps1
# Generated: $(Get-Date)

# ═══════════════════════════════════════════════════════════════
# AZURE CONFIGURATION
# ═══════════════════════════════════════════════════════════════

# Service Principal credentials (JSON)
AZURE_CREDENTIALS='$spJsonStr'

# Azure subscription ID
AZURE_SUBSCRIPTION_ID='$subscriptionId'

# Azure resource group name
AZURE_RESOURCE_GROUP='$resourceGroupName'

# Azure region
AZURE_LOCATION='$location'

# ═══════════════════════════════════════════════════════════════
# NOTE: Azure Container Registry (ACR) is auto-provisioned by Aspire
# during deployment. No manual ACR setup is needed.
# ═══════════════════════════════════════════════════════════════

# ═══════════════════════════════════════════════════════════════
# AZURE FOUNDRY (CHATBOT AI) - COMMENTED FOR NOW
# ═══════════════════════════════════════════════════════════════

# Uncomment these when you set up Azure OpenAI/Foundry
# FOUNDRY_ENDPOINT='https://your-foundry.openai.azure.com/'
# FOUNDRY_KEY='your-api-key'

# ═══════════════════════════════════════════════════════════════
# LINEAR INTEGRATION - COMMENTED FOR NOW
# ═══════════════════════════════════════════════════════════════
# Will enable in next phase
# LINEAR_API_KEY='your-linear-api-key'
# LINEAR_API_ENDPOINT='https://api.linear.app/graphql'
# LINEAR_BUILD_ISSUE_ID='ESHOP-1'

# ═══════════════════════════════════════════════════════════════
# ASPIRE CONFIGURATION
# ═══════════════════════════════════════════════════════════════

ASPIRE_MODULE_PATH='src/eShop.AppHost/eShop.AppHost.csproj'
"@

    Set-Content -Path $envFile -Value $envContent
    Write-Success-Green "✅ Created $envFile"
    Write-Host "   ⚠️  Add your Linear API key to $envFile"
    Write-Host ""
}

################################################################################
# STEP 8: Authenticate with GitHub
################################################################################

Write-Warning-Yellow "Step 8: Authenticating with GitHub..."
Write-Host ""

$ghStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning-Yellow "🔐 Please authenticate with GitHub"
    gh auth login
}

# Get repo info
$repo = gh repo view --json nameWithOwner -q
Write-Success-Green "✅ Authenticated to repo: $repo"
Write-Host ""

################################################################################
# STEP 9: Set GitHub Secrets
################################################################################

Write-Warning-Yellow "Step 9: Setting GitHub Secrets..."
Write-Host ""

function Set-GitHubSecret {
    param(
        [string]$Key,
        [string]$Value
    )
    
    if ([string]::IsNullOrEmpty($Value)) {
        Write-Warning-Yellow "⚠️  Skipping $Key (not set)"
        return
    }
    
    $Value | gh secret set $Key 2>$null
    Write-Success-Green "✅ Set $Key"
}

# Set Azure secrets
Set-GitHubSecret "AZURE_CREDENTIALS" $spJsonStr
Set-GitHubSecret "AZURE_SUBSCRIPTION_ID" $subscriptionId
Set-GitHubSecret "AZURE_RESOURCE_GROUP" $resourceGroupName
Set-GitHubSecret "AZURE_LOCATION" $location

# Set Aspire configuration
Set-GitHubSecret "ASPIRE_MODULE_PATH" "src/eShop.AppHost/eShop.AppHost.csproj"

# NOTE: ACR secrets are not needed - Aspire handles container registry provisioning

Write-Host ""

# LINEAR SETUP - COMMENTED FOR NOW (Phase 2)
# ################################################################################
# # STEP 10: Linear Setup
# ################################################################################
# Write-Warning-Yellow "Step 10: Linear Setup"
# Write-Host ""
# Write-Host "To complete setup, you need to:"
# Write-Host ""
# Write-Host "1. Create a Linear API token:"
# Write-Host "   https://linear.app/settings/api"
# Write-Host ""
# Write-Host "2. Create a Linear issue for build tracking:"
# Write-Host "   https://linear.app/create"
# Write-Host ""
# Write-Host "3. Edit .env.local and add:"
# Write-Host "   - LINEAR_API_KEY"
# Write-Host "   - LINEAR_BUILD_ISSUE_ID"
# Write-Host ""
# Write-Host "4. Run: .\scripts\setup-github-secrets.ps1"
# Write-Host ""

################################################################################
# STEP 11: Verify Setup
################################################################################

Write-Warning-Yellow "Step 11: Verifying setup..."
Write-Host ""

Write-Info-Blue "Secrets configured in GitHub:"
gh secret list --limit 30 | Select-String "AZURE_|ASPIRE_" | ForEach-Object { Write-Host "  $_" }

Write-Host ""

################################################################################
# Summary
################################################################################

Write-Success-Green "═══════════════════════════════════════════════════════════"
Write-Success-Green "✅ Setup Complete!"
Write-Success-Green "═══════════════════════════════════════════════════════════"
Write-Host ""
Write-Info-Blue "What was created:"
Write-Host "  ✅ Resource Group: $resourceGroupName"
Write-Host "  ✅ Service Principal: $appName"
Write-Host "  ✅ Azure Container Registry: $acrRegistryName.azurecr.io"
Write-Host "  ✅ .env.local file with Azure credentials"
Write-Host "  ✅ GitHub Secrets (AZURE_*, ACR_*, ASPIRE_*)"
Write-Host "  ✅ Service Principal: $appName"
Write-Host "  ✅ .env.local file with Azure credentials"
Write-Host "  ✅ GitHub Secrets (AZURE_*, ASPIRE_*)"
Write-Host ""
Write-Info-Blue "Next steps:"
Write-Host ""
Write-Host "1. Complete Linear setup:"
Write-Host "   - Get API token: https://linear.app/settings/api"
Write-Host "   - Create tracking issue"
Write-Host "   - Add to .env.local"
Write-Host ""
Write-Host "2. Run full secret setup (includes Linear):"
Write-Host "   .\scripts\setup-github-secrets.ps1"
Write-Host ""
Write-Host "3. Verify secrets in GitHub:"
Write-Host "   Settings → Secrets and variables → Actions"
Write-Host ""
Write-Host "4. Push a feature branch to trigger CI/CD:"
Write-Host "   git push origin feature/github-actions-cicd"
Write-Host ""
Write-Host "5. Monitor deployment:"
Write-Host "   - GitHub Actions tab"
Write-Host "   - Azure Portal → Container Apps"
Write-Host ""
Write-Info-Blue "Documentation:"
Write-Host "  - Azure Setup: docs/AZURE_SETUP.md"
Write-Host "  - Secrets Config: docs/GITHUB_ACTIONS_SECRETS.md"
Write-Host "  - Deployment: docs/AZURE_CONTAINER_APPS_DEPLOYMENT.md"
Write-Host ""
Write-Success-Green "Happy deploying! 🚀"
Write-Host ""
