# Azure Configuration Setup

This guide explains how to configure Azure for the GitHub Actions CI/CD pipeline using Azure Container Registry (ACR) for Docker images.

## Quick Start (Recommended)

Use the automated setup scripts to configure everything in minutes:

### Windows (PowerShell)
```powershell
.\scripts\setup-azure-and-github.ps1
```

### macOS/Linux (Bash)
```bash
chmod +x ./scripts/setup-azure-and-github.sh
./scripts/setup-azure-and-github.sh
```

The script will:
1. ✅ Create Resource Group
2. ✅ Create Service Principal
3. ✅ Create Azure Container Registry (ACR)
4. ✅ Generate `.env.local` with all credentials
5. ✅ Configure GitHub Secrets

**If using automation, skip to "Verification" section.**

---

## Prerequisites (Manual Setup)

1. Azure Account with active subscription
2. Azure CLI installed locally
3. GitHub repository access
4. GitHub CLI installed
5. jq installed (for JSON parsing)

---

## Manual Setup (Advanced)

### Step 1: Create Azure Resource Group

```bash
# Login to Azure
az login

# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Create resource group
az group create \
  --name "eshop-marketplace" \
  --location "eastus"
```

### Step 2: Create Service Principal

```bash
# Create service principal for GitHub
az ad sp create-for-rbac \
  --name "github-actions-eshop-marketplace" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth
```

This outputs JSON:
```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "..."
}
```

**Save this entire JSON** - you'll use it for `AZURE_CREDENTIALS` secret.

### Step 3: Create Azure Container Registry (ACR)

```bash
# Create ACR
az acr create \
  --resource-group "eshop-marketplace" \
  --name "eshopmarketplaceacr" \
  --sku Standard

# Get ACR credentials
az acr credential show \
  --name "eshopmarketplaceacr" \
  --resource-group "eshop-marketplace"
```

Output:
```json
{
  "passwords": [
    { "name": "password", "value": "..." }
  ],
  "username": "..."
}
```

Save:
- `username` → `ACR_USERNAME`
- `passwords[0].value` → `ACR_PASSWORD`
- Registry name → `ACR_REGISTRY_NAME`

### Step 4: Create `.env.local`

```bash
cp .env.example .env.local
```

Edit `.env.local`:
```bash
AZURE_SUBSCRIPTION_ID='your-subscription-id'
AZURE_CREDENTIALS='{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}'
AZURE_RESOURCE_GROUP='eshop-marketplace'
AZURE_LOCATION='eastus'

# ACR - New!
ACR_REGISTRY_NAME='eshopmarketplaceacr'
ACR_USERNAME='your-acr-username'
ACR_PASSWORD='your-acr-password'

# Linear
LINEAR_API_KEY='your-linear-api-key'
LINEAR_BUILD_ISSUE_ID='ESHOP-1'

# Aspire
ASPIRE_MODULE_PATH='src/eShop.AppHost/eShop.AppHost.csproj'
```

### Step 5: Set GitHub Secrets

```bash
# Automated
./scripts/setup-github-secrets.sh

# Or manually
gh secret set AZURE_CREDENTIALS --body "$(grep AZURE_CREDENTIALS .env.local | cut -d= -f2)"
gh secret set AZURE_SUBSCRIPTION_ID --body "your-subscription-id"
gh secret set AZURE_RESOURCE_GROUP --body "eshop-marketplace"
gh secret set AZURE_LOCATION --body "eastus"
gh secret set ACR_REGISTRY_NAME --body "eshopmarketplaceacr"
gh secret set ACR_USERNAME --body "your-acr-username"
gh secret set ACR_PASSWORD --body "your-acr-password"
gh secret set ASPIRE_MODULE_PATH --body "src/eShop.AppHost/eShop.AppHost.csproj"
```

---

## Verification

```bash
#!/bin/bash
set -e

# Load secrets from .env.local
if [ ! -f .env.local ]; then
  echo "❌ .env.local not found! Create it first."
  exit 1
fi

set -a
source .env.local
set +a

echo "🔧 Setting up GitHub secrets..."

# Azure secrets
gh secret set AZURE_CREDENTIALS --body "$AZURE_CREDENTIALS"
gh secret set AZURE_SUBSCRIPTION_ID --body "$AZURE_SUBSCRIPTION_ID"
gh secret set AZURE_RESOURCE_GROUP --body "$AZURE_RESOURCE_GROUP"
gh secret set AZURE_LOCATION --body "$AZURE_LOCATION"

# Foundry secrets
gh secret set FOUNDRY_ENDPOINT --body "$FOUNDRY_ENDPOINT"
gh secret set FOUNDRY_KEY --body "$FOUNDRY_KEY"

# Linear secrets
gh secret set LINEAR_API_KEY --body "$LINEAR_API_KEY"
gh secret set LINEAR_API_ENDPOINT --body "$LINEAR_API_ENDPOINT"
gh secret set LINEAR_BUILD_ISSUE_ID --body "$LINEAR_BUILD_ISSUE_ID"

# Aspire
gh secret set ASPIRE_MODULE_PATH --body "$ASPIRE_MODULE_PATH"

echo "✅ All secrets configured successfully!"
echo ""
echo "📋 Verify secrets were set:"
gh secret list
```

## Step 8: Test Azure Configuration

```bash
# Login with the service principal
az login --service-principal \
  -u $(jq -r .clientId <<< "$AZURE_CREDENTIALS") \
  -p $(jq -r .clientSecret <<< "$AZURE_CREDENTIALS") \
  --tenant $(jq -r .tenantId <<< "$AZURE_CREDENTIALS")

# List resource groups
az group list --query "[].name" -o table

# Verify Foundry resource
az cognitiveservices account show \
  --name eshop-foundry \
  --resource-group eshop-marketplace
```

## Environment-Specific Configuration

### Staging Environment (Optional)

For staging deployments, create these additional secrets:

```bash
AZURE_RESOURCE_GROUP_STAGING='eshop-marketplace-staging'
AZURE_LOCATION_STAGING='eastus'
```

Then in GitHub, set as **Environment secrets**:
- Settings → Environments → staging → Environment secrets

### Production Environment

Set these with **Environment protection rules**:
1. Settings → Environments → production
2. Add required reviewers
3. Add these environment secrets:
   - `AZURE_RESOURCE_GROUP_PROD`
   - `AZURE_LOCATION_PROD`

## Troubleshooting

### "Invalid Azure credentials"
```bash
# Verify credentials are valid JSON
echo "$AZURE_CREDENTIALS" | jq empty
# Should not error

# Check service principal exists
az ad sp show --id $(jq -r .clientId <<< "$AZURE_CREDENTIALS")
```

### "Insufficient permissions"
```bash
# Check role assignment
az role assignment list --all \
  --query "[?principalName=='github-actions-eshop-marketplace']"

# Add Contributor role if missing
az role assignment create \
  --role Contributor \
  --assignee $(jq -r .clientId <<< "$AZURE_CREDENTIALS") \
  --scope /subscriptions/{SUBSCRIPTION_ID}
```

### "Foundry endpoint not found"
- Verify resource name is correct
- Check region matches `AZURE_LOCATION`
- Ensure Azure OpenAI is available in that region

## Security Notes

⚠️ **NEVER:**
- Commit `.env.local` to git
- Share `AZURE_CREDENTIALS` or `FOUNDRY_KEY` in chat/email
- Use these credentials locally in production code
- Store secrets in environment variables outside CI/CD

✅ **DO:**
- Rotate credentials every 90 days
- Use managed identities when possible
- Audit Azure Activity Log for secret access
- Review GitHub Actions logs for errors (hide secrets output)

## Next Steps

1. ✅ Create service principal and get AZURE_CREDENTIALS
2. ✅ Create resource group and save name
3. ✅ Deploy Azure OpenAI for Foundry
4. ✅ Create `.env.local` with all values
5. ✅ Set GitHub secrets
6. ✅ Test by running GitHub Actions workflow

Once complete, your CI/CD pipeline can automatically deploy to Azure! 🚀
