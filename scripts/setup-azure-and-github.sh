#!/bin/bash

################################################################################
# eShop Marketplace - Automated Azure & GitHub Setup
#
# This script automates:
# 1. Azure Resource Group creation
# 2. Azure Service Principal creation
# 3. GitHub Secrets configuration
# 4. .env.local file generation
#
# Usage: ./scripts/setup-azure-and-github.sh
#
################################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
RESOURCE_GROUP_NAME="eshop-marketplace"
APP_NAME="eshop-marketplace"
LOCATION="eastus"
ENVIRONMENT="staging"

# Banner
echo -e "${BLUE}"
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  eShop Marketplace - Automated Azure & GitHub Setup          ║"
echo "║  Automates: Resource Group, Service Principal, Secrets       ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

################################################################################
# STEP 1: Check Prerequisites
################################################################################

echo -e "${YELLOW}Step 1: Checking prerequisites...${NC}"
echo ""

# Check Azure CLI
if ! command -v az &> /dev/null; then
  echo -e "${RED}❌ Azure CLI not found!${NC}"
  echo "Install from: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"
  exit 1
fi
echo -e "${GREEN}✅ Azure CLI installed${NC}"

# Check GitHub CLI
if ! command -v gh &> /dev/null; then
  echo -e "${RED}❌ GitHub CLI not found!${NC}"
  echo "Install from: https://cli.github.com/"
  exit 1
fi
echo -e "${GREEN}✅ GitHub CLI installed${NC}"

# Check jq for JSON parsing
if ! command -v jq &> /dev/null; then
  echo -e "${RED}❌ jq not found!${NC}"
  echo "Install from: https://stedolan.github.io/jq/download/"
  exit 1
fi
echo -e "${GREEN}✅ jq installed${NC}"

echo ""

################################################################################
# STEP 2: Azure Authentication
################################################################################

echo -e "${YELLOW}Step 2: Authenticating with Azure...${NC}"
echo ""

if ! az account show &> /dev/null; then
  echo -e "${YELLOW}🔐 Please authenticate with Azure${NC}"
  az login
  echo ""
fi

# Get subscription info
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)

echo -e "${GREEN}✅ Authenticated${NC}"
echo "   Subscription: $SUBSCRIPTION_NAME"
echo "   ID: $SUBSCRIPTION_ID"
echo ""

################################################################################
# STEP 3: Gather User Input
################################################################################

echo -e "${YELLOW}Step 3: Configuration (press Enter to use defaults)${NC}"
echo ""

read -p "Resource Group Name [$RESOURCE_GROUP_NAME]: " input_rg
RESOURCE_GROUP_NAME="${input_rg:-$RESOURCE_GROUP_NAME}"

read -p "Location [$LOCATION]: " input_location
LOCATION="${input_location:-$LOCATION}"

read -p "App/Service Principal Name [$APP_NAME]: " input_app
APP_NAME="${input_app:-$APP_NAME}"

ACR_NAME="eshop-marketplace-acr"
read -p "ACR Registry Name [$ACR_NAME]: " input_acr
ACR_NAME="${input_acr:-$ACR_NAME}"

echo ""
echo -e "${BLUE}Summary:${NC}"
echo "  Resource Group: $RESOURCE_GROUP_NAME"
echo "  Location: $LOCATION"
echo "  Service Principal: $APP_NAME"
echo "  ACR Registry: $ACR_NAME"
echo "  Subscription: $SUBSCRIPTION_ID"
echo ""

read -p "Continue? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo -e "${RED}Cancelled${NC}"
  exit 1
fi

echo ""

################################################################################
# STEP 4: Create Azure Resource Group
################################################################################

echo -e "${YELLOW}Step 4: Creating Azure Resource Group...${NC}"
echo ""

# Check if resource group already exists
if az group exists --name "$RESOURCE_GROUP_NAME" | grep -q true; then
  echo -e "${YELLOW}⚠️  Resource group '$RESOURCE_GROUP_NAME' already exists${NC}"
else
  echo "Creating resource group..."
  az group create \
    --name "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION" \
    --output none
  echo -e "${GREEN}✅ Resource group created${NC}"
fi

echo ""

################################################################################
# STEP 5: Create Azure Service Principal
################################################################################

echo -e "${YELLOW}Step 5: Creating Azure Service Principal...${NC}"
echo ""

# Check if service principal already exists
if az ad sp list --filter "displayName eq '$APP_NAME'" --query "[0].id" -o tsv 2>/dev/null | grep -q .; then
  echo -e "${YELLOW}⚠️  Service principal '$APP_NAME' already exists${NC}"
  SP_ID=$(az ad sp list --filter "displayName eq '$APP_NAME'" --query "[0].id" -o tsv)
  echo "Using existing service principal: $SP_ID"
else
  echo "Creating service principal..."
  
  # Create service principal and get credentials
  SP_JSON=$(az ad sp create-for-rbac \
    --name "$APP_NAME" \
    --role contributor \
    --scopes "/subscriptions/$SUBSCRIPTION_ID" \
    --sdk-auth)
  
  echo -e "${GREEN}✅ Service principal created${NC}"
fi

# Extract credentials from JSON
if [ -z "$SP_JSON" ]; then
  SP_JSON=$(az ad sp credential reset \
    --id "$SP_ID" \
    --credential-description "github-actions" \
    --sdk-auth)
fi

CLIENT_ID=$(echo "$SP_JSON" | jq -r '.clientId')
CLIENT_SECRET=$(echo "$SP_JSON" | jq -r '.clientSecret')
TENANT_ID=$(echo "$SP_JSON" | jq -r '.tenantId')

echo ""
echo -e "${BLUE}Service Principal Details:${NC}"
echo "  Client ID: $CLIENT_ID"
echo "  Tenant ID: $TENANT_ID"
echo ""

################################################################################
# STEP 6: Create Azure Container Registry (ACR)
################################################################################

echo -e "${YELLOW}Step 6: Setting up Azure Container Registry...${NC}"
echo ""

# Check if ACR already exists
if az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP_NAME" &>/dev/null 2>&1; then
  echo -e "${YELLOW}⚠️  ACR '$ACR_NAME' already exists${NC}"
else
  echo "Creating ACR registry..."
  az acr create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$ACR_NAME" \
    --sku Standard \
    --output none
  echo -e "${GREEN}✅ ACR created${NC}"
fi

# Get ACR login credentials
ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP_NAME" --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP_NAME" --query passwords[0].value -o tsv)
ACR_REGISTRY_NAME=$ACR_NAME

echo ""
echo -e "${BLUE}ACR Details:${NC}"
echo "  Registry: $ACR_REGISTRY_NAME.azurecr.io"
echo "  Username: $ACR_USERNAME"
echo ""

################################################################################
# STEP 7: Create .env.local
################################################################################

echo -e "${YELLOW}Step 7: Creating .env.local file...${NC}"
echo ""

ENV_FILE=".env.local"

# Check if .env.local already exists
if [ -f "$ENV_FILE" ]; then
  echo -e "${YELLOW}⚠️  $ENV_FILE already exists${NC}"
  read -p "Overwrite? (y/n) " -n 1 -r
  echo ""
  if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Skipping .env.local creation"
    ENV_FILE=""
  fi
fi

if [ ! -z "$ENV_FILE" ]; then
  cat > "$ENV_FILE" << EOF
# Auto-generated by setup-azure-and-github.sh
# Generated: $(date)

# ═══════════════════════════════════════════════════════════════
# AZURE CONFIGURATION
# ═══════════════════════════════════════════════════════════════

# Service Principal credentials (JSON)
AZURE_CREDENTIALS='$SP_JSON'

# Azure subscription ID
AZURE_SUBSCRIPTION_ID='$SUBSCRIPTION_ID'

# Azure resource group name
AZURE_RESOURCE_GROUP='$RESOURCE_GROUP_NAME'

# Azure region
AZURE_LOCATION='$LOCATION'

# ═══════════════════════════════════════════════════════════════
# AZURE CONTAINER REGISTRY (ACR)
# ═══════════════════════════════════════════════════════════════

ACR_REGISTRY_NAME='$ACR_REGISTRY_NAME'
ACR_USERNAME='$ACR_USERNAME'
ACR_PASSWORD='$ACR_PASSWORD'

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
EOF

  echo -e "${GREEN}✅ Created $ENV_FILE${NC}"
  echo ""
  echo "   Note: Linear integration is commented for now"
  echo ""
fi

################################################################################
# STEP 8: Authenticate with GitHub
################################################################################

echo -e "${YELLOW}Step 8: Authenticating with GitHub...${NC}"
echo ""

if ! gh auth status &> /dev/null; then
  echo -e "${YELLOW}🔐 Please authenticate with GitHub${NC}"
  gh auth login
  echo ""
fi

# Get repo info
REPO=$(gh repo view --json nameWithOwner -q)
echo -e "${GREEN}✅ Authenticated to repo: $REPO${NC}"
echo ""

################################################################################
# STEP 9: Set GitHub Secrets
################################################################################

echo -e "${YELLOW}Step 9: Setting GitHub Secrets...${NC}"
echo ""

# Function to set secret safely
set_secret() {
  local key=$1
  local value=$2
  
  if [ -z "$value" ]; then
    echo -e "${YELLOW}⚠️  Skipping $key (not set)${NC}"
    return
  fi
  
  gh secret set "$key" --body "$value" 2>/dev/null
  echo -e "${GREEN}✅ Set $key${NC}"
}

# Set Azure secrets
set_secret "AZURE_CREDENTIALS" "$SP_JSON"
set_secret "AZURE_SUBSCRIPTION_ID" "$SUBSCRIPTION_ID"
set_secret "AZURE_RESOURCE_GROUP" "$RESOURCE_GROUP_NAME"
set_secret "AZURE_LOCATION" "$LOCATION"

# Set ACR secrets
set_secret "ACR_REGISTRY_NAME" "$ACR_REGISTRY_NAME"
set_secret "ACR_USERNAME" "$ACR_USERNAME"
set_secret "ACR_PASSWORD" "$ACR_PASSWORD"

# Set other required secrets
set_secret "ASPIRE_MODULE_PATH" "src/eShop.AppHost/eShop.AppHost.csproj"

echo ""

# LINEAR SETUP - COMMENTED FOR NOW (Phase 2)
# ################################################################################
# # STEP 10: Linear Setup
# ################################################################################
# echo -e "${YELLOW}Step 10: Linear Setup${NC}"
# echo ""
# echo "To complete setup, you need to:"
# echo ""
# echo "1. Create a Linear API token:"
# echo "   https://linear.app/settings/api"
# echo ""
# echo "2. Create a Linear issue for build tracking:"
# echo "   https://linear.app/create"
# echo ""
# echo "3. Edit .env.local and add:"
# echo "   - LINEAR_API_KEY"
# echo "   - LINEAR_BUILD_ISSUE_ID"
# echo ""
# echo "4. Run: ./scripts/setup-github-secrets.sh"
# echo ""

################################################################################
# STEP 11: Verify Setup
################################################################################

echo -e "${YELLOW}Step 11: Verifying setup...${NC}"
echo ""

echo -e "${BLUE}Secrets configured in GitHub:${NC}"
gh secret list --limit 30 | grep -E "AZURE_|ACR_|ASPIRE_" || echo "No secrets found"

echo ""

################################################################################
# Summary
################################################################################

echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}What was created:${NC}"
echo "  ✅ Resource Group: $RESOURCE_GROUP_NAME"
echo "  ✅ Service Principal: $APP_NAME"
echo "  ✅ Azure Container Registry: $ACR_REGISTRY_NAME.azurecr.io"
echo "  ✅ .env.local file with Azure credentials"
echo "  ✅ GitHub Secrets (AZURE_*, ASPIRE_*)"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo ""
echo "1. Complete Linear setup:"
echo "   - Get API token: https://linear.app/settings/api"
echo "   - Create tracking issue"
echo "   - Add to .env.local"
echo ""
echo "2. Run full secret setup (includes Linear):"
echo "   ./scripts/setup-github-secrets.sh"
echo ""
echo "3. Verify secrets in GitHub:"
echo "   Settings → Secrets and variables → Actions"
echo ""
echo "4. Push a feature branch to trigger CI/CD:"
echo "   git push origin feature/github-actions-cicd"
echo ""
echo "5. Monitor deployment:"
echo "   - GitHub Actions tab"
echo "   - Azure Portal → Container Apps"
echo ""
echo -e "${BLUE}Documentation:${NC}"
echo "  - Azure Setup: docs/AZURE_SETUP.md"
echo "  - Secrets Config: docs/GITHUB_ACTIONS_SECRETS.md"
echo "  - Deployment: docs/AZURE_CONTAINER_APPS_DEPLOYMENT.md"
echo ""
echo -e "${GREEN}Happy deploying! 🚀${NC}"
echo ""
