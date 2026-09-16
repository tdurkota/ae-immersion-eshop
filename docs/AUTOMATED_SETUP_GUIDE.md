# Automated Setup Guide

This document explains how to use the automated setup scripts to configure Azure and GitHub for the eShop Marketplace CI/CD pipeline.

## Quick Start

### For Windows (PowerShell)
```powershell
.\scripts\setup-azure-and-github.ps1
```

### For macOS/Linux (Bash)
```bash
chmod +x ./scripts/setup-azure-and-github.sh
./scripts/setup-azure-and-github.sh
```

## What Gets Automated

### Script 1: `setup-azure-and-github.sh` / `setup-azure-and-github.ps1`

**Automates:**
- ✅ Azure authentication
- ✅ Resource group creation
- ✅ Service principal creation
- ✅ Azure Container Registry (ACR) creation
- ✅ .env.local file generation
- ✅ GitHub secrets configuration (Azure + ACR)

**Prerequisites:**
- Azure CLI installed
- GitHub CLI installed
- jq installed (for JSON parsing - Bash only)
- Azure subscription
- GitHub repository access

**Output:**
- `.env.local` - Environment variables for local development
- GitHub Secrets:
  - `AZURE_CREDENTIALS`
  - `AZURE_SUBSCRIPTION_ID`
  - `AZURE_RESOURCE_GROUP`
  - `AZURE_LOCATION`
  - `ACR_REGISTRY_NAME`
  - `ACR_USERNAME`
  - `ACR_PASSWORD`
  - `ASPIRE_MODULE_PATH`

**Time:** ~2-3 minutes

---

### Script 2: `setup-github-secrets.sh` / `setup-github-secrets.ps1`

**Automates:**
- ✅ Reading from `.env.local`
- ✅ Setting ALL GitHub secrets (including Linear)

**Prerequisites:**
- `.env.local` file exists
- GitHub CLI installed
- All values in `.env.local` are filled

**Output:**
- All secrets from `.env.local` pushed to GitHub

**Time:** ~30 seconds

---

## Detailed Workflow

### Step 1: Run Main Setup Script

**Windows:**
```powershell
.\scripts\setup-azure-and-github.ps1
```

**macOS/Linux:**
```bash
./scripts/setup-azure-and-github.sh
```

**What happens:**
1. Checks Azure CLI, GitHub CLI, jq installed
2. Authenticates with Azure (opens browser if needed)
3. Prompts for configuration:
   - Resource Group Name
   - Azure Location
   - Service Principal Name
   - ACR Registry Name
4. Creates Azure resources (or uses existing)
5. Creates ACR and gets credentials
6. Generates `.env.local` with all credentials
7. Sets GitHub secrets (Azure + ACR)

### Step 2: Configure Linear Integration

1. Get Linear API token:
   - Go to https://linear.app/settings/api
   - Click "Create API key"
   - Copy token

2. Create Linear issue for build tracking:
   - Go to https://linear.app
   - Create new issue (note the ID, e.g., `ESHOP-1`)

3. Edit `.env.local`:
   ```bash
   LINEAR_API_KEY='lin_api_xxxxxxxxxxxxx'
   LINEAR_BUILD_ISSUE_ID='ESHOP-1'
   ```

### Step 3: Run Secrets Setup Script

**Windows:**
```powershell
.\scripts\setup-github-secrets.ps1
```

**macOS/Linux:**
```bash
./scripts/setup-github-secrets.sh
```

**What happens:**
1. Reads all values from `.env.local`
2. Sets every secret in GitHub
3. Lists all configured secrets

### Step 4: Verify Secrets

Visit GitHub repo:
```
Settings → Secrets and variables → Actions
```

Should see:
- AZURE_CREDENTIALS
- AZURE_SUBSCRIPTION_ID
- AZURE_RESOURCE_GROUP
- AZURE_LOCATION
- FOUNDRY_ENDPOINT (commented for now)
- FOUNDRY_KEY (commented for now)
- LINEAR_API_KEY
- LINEAR_API_ENDPOINT
- LINEAR_BUILD_ISSUE_ID
- ASPIRE_MODULE_PATH

### Step 5: Test the Setup

Push a feature branch:
```bash
git checkout feature/github-actions-cicd
git push origin feature/github-actions-cicd
```

Watch GitHub Actions:
```bash
gh run list
gh run view {RUN_ID} --log
```

Or visit: `https://github.com/{owner}/{repo}/actions`

---

## Troubleshooting

### "Azure CLI not found"
**Solution:** Install Azure CLI
- Windows: `choco install azure-cli` (or download from Microsoft)
- macOS: `brew install azure-cli`
- Linux: Follow https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-linux

### "GitHub CLI not found"
**Solution:** Install GitHub CLI
- Visit: https://cli.github.com/
- Or: `brew install gh` (macOS), `choco install gh` (Windows)

### "Permission denied" on .sh script
**Solution:** Make executable
```bash
chmod +x ./scripts/setup-azure-and-github.sh
chmod +x ./scripts/setup-github-secrets.sh
```

### "Resource group already exists"
**Normal behavior!** The script will skip creation and use existing group.

### "Service principal already exists"
**Normal behavior!** The script will reset credentials and use existing principal.

### "Secret already exists"
**Normal behavior!** GitHub will update the existing secret.

### "Linear API key invalid"
**Solution:** 
1. Check API token is copied correctly
2. Verify it's not revoked at https://linear.app/settings/api
3. Create a new token if needed

### ".env.local" already exists
**You can choose:**
- Keep existing (skip creation)
- Overwrite with new credentials

⚠️ **BACKUP FIRST** if it has credentials you want to keep!

---

## Manual Alternative

If you prefer manual setup without scripts:

### 1. Create Resource Group
```bash
az group create --name eshop-marketplace --location eastus
```

### 2. Create Service Principal
```bash
az ad sp create-for-rbac \
  --name "github-actions-eshop-marketplace" \
  --role contributor \
  --scopes /subscriptions/{SUBSCRIPTION_ID} \
  --sdk-auth
```

### 3. Create .env.local
```bash
cp .env.example .env.local
# Edit with your credentials
```

### 4. Set GitHub Secrets
```bash
gh secret set AZURE_CREDENTIALS --body "$(cat .env.local | grep AZURE_CREDENTIALS)"
# ... repeat for each secret
```

---

## Security Notes

✅ **Safe Practices:**
- `.env.local` is in `.gitignore` (never committed)
- Credentials are stored only in GitHub encrypted secrets
- Service principal has only Contributor role to specific resource group
- GitHub CLI encrypts secrets before sending

⚠️ **Keep Secure:**
- Never share `.env.local`
- Never commit `.env.local` to git
- Never paste secrets in chat/email
- Rotate credentials every 90 days
- Monitor Azure Activity Log for suspicious access

---

## What's Next?

After setup completes:

1. **Create a PR** with your feature branch
2. **Monitor the workflow** in GitHub Actions
3. **Check deployment** on Azure Portal
4. **Test the application** at the deployed URL

Example flow:
```bash
# Already done - feature branch created with setup
git push origin feature/github-actions-cicd

# Create PR
gh pr create --base develop --head feature/github-actions-cicd --title "feat: Add GitHub Actions CI/CD" --body "Automated CI/CD pipeline setup"

# Watch workflow
gh run list
gh run view {RUN_ID} --log
```

---

## File Reference

| File | Purpose |
|------|---------|
| `.env.example` | Template for `.env.local` |
| `.env.local` | Local secrets (git-ignored) |
| `scripts/setup-azure-and-github.sh` | Main setup (Bash) |
| `scripts/setup-azure-and-github.ps1` | Main setup (PowerShell) |
| `scripts/setup-github-secrets.sh` | Secrets sync (Bash) |
| `scripts/setup-github-secrets.ps1` | Secrets sync (PowerShell) |
| `.gitignore` | Excludes sensitive files |
| `docs/AZURE_SETUP.md` | Detailed Azure guide |
| `docs/GITHUB_ACTIONS_SECRETS.md` | Secrets reference |
| `docs/AZURE_CONTAINER_APPS_DEPLOYMENT.md` | Deployment guide |

---

## Support

For detailed information:
- See `docs/AZURE_SETUP.md` for Azure configuration
- See `docs/GITHUB_ACTIONS_SECRETS.md` for all secrets
- See `docs/AZURE_CONTAINER_APPS_DEPLOYMENT.md` for deployment
- Run: `az --help` or `gh --help`

Good luck! 🚀
