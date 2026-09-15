#!/bin/bash
set -e

# GitHub Actions Secrets Setup Script
# This script loads secrets from .env.local and sets them in GitHub

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🔧 GitHub Actions Secrets Setup${NC}"
echo "================================="
echo ""

# Check if .env.local exists
if [ ! -f .env.local ]; then
  echo -e "${RED}❌ Error: .env.local not found!${NC}"
  echo ""
  echo "Please create .env.local first with your Azure and other credentials."
  echo "See docs/AZURE_SETUP.md for instructions."
  exit 1
fi

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
  echo -e "${RED}❌ Error: GitHub CLI (gh) is not installed!${NC}"
  echo ""
  echo "Install it from: https://cli.github.com/"
  exit 1
fi

# Check if authenticated with GitHub
if ! gh auth status &> /dev/null; then
  echo -e "${RED}❌ Error: Not authenticated with GitHub!${NC}"
  echo ""
  echo "Run: gh auth login"
  exit 1
fi

# Load secrets from .env.local
echo -e "${YELLOW}📂 Loading secrets from .env.local...${NC}"
set -a
source .env.local
set +a

echo ""

# Function to set secret safely
set_secret() {
  local key=$1
  local value=$2
  
  if [ -z "$value" ]; then
    echo -e "${YELLOW}⚠️  Skipping $key (not set in .env.local)${NC}"
    return
  fi
  
  gh secret set "$key" --body "$value" 2>/dev/null
  echo -e "${GREEN}✅ Set $key${NC}"
}

# Set Azure Secrets
echo -e "${YELLOW}📦 Setting Azure secrets...${NC}"
set_secret "AZURE_CREDENTIALS" "$AZURE_CREDENTIALS"
set_secret "AZURE_SUBSCRIPTION_ID" "$AZURE_SUBSCRIPTION_ID"
set_secret "AZURE_RESOURCE_GROUP" "$AZURE_RESOURCE_GROUP"
set_secret "AZURE_LOCATION" "$AZURE_LOCATION"

echo ""
echo -e "${YELLOW}🤖 Setting Foundry (Chatbot) secrets...${NC}"
set_secret "FOUNDRY_ENDPOINT" "$FOUNDRY_ENDPOINT"
set_secret "FOUNDRY_KEY" "$FOUNDRY_KEY"

echo ""
echo -e "${YELLOW}📋 Setting Linear secrets...${NC}"
set_secret "LINEAR_API_KEY" "$LINEAR_API_KEY"
set_secret "LINEAR_API_ENDPOINT" "$LINEAR_API_ENDPOINT"
set_secret "LINEAR_BUILD_ISSUE_ID" "$LINEAR_BUILD_ISSUE_ID"

echo ""
echo -e "${YELLOW}🎛️  Setting Aspire secrets...${NC}"
set_secret "ASPIRE_MODULE_PATH" "$ASPIRE_MODULE_PATH"

echo ""
echo -e "${GREEN}═════════════════════════════════${NC}"
echo -e "${GREEN}✅ All secrets configured!${NC}"
echo -e "${GREEN}═════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}📋 Verifying secrets (names only):${NC}"
gh secret list

echo ""
echo -e "${GREEN}🎉 Setup complete!${NC}"
echo ""
echo "Next steps:"
echo "1. Verify secrets are correct in GitHub Settings"
echo "2. Push your feature branch: git push origin feature/github-actions-cicd"
echo "3. Create a pull request"
echo "4. GitHub Actions will automatically test the configuration"
echo ""
echo "For more info, see: docs/AZURE_SETUP.md and docs/GITHUB_ACTIONS_SECRETS.md"
