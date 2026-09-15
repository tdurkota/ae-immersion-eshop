# GitHub Actions CI/CD Setup - Quick Start

## TL;DR - Get Started in 5 Minutes

### Prerequisites
- Azure CLI: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
- GitHub CLI: https://cli.github.com/
- Azure subscription
- GitHub repo access

### Quick Setup

**Windows (PowerShell):**
```powershell
.\scripts\setup-azure-and-github.ps1
```

**macOS/Linux (Bash):**
```bash
chmod +x ./scripts/setup-azure-and-github.sh
./scripts/setup-azure-and-github.sh
```

**Then configure Linear:**
1. Get API key: https://linear.app/settings/api
2. Edit `.env.local` and add `LINEAR_API_KEY` and `LINEAR_BUILD_ISSUE_ID`
3. Run: `./scripts/setup-github-secrets.sh` (or `.ps1` on Windows)

**Push to GitHub:**
```bash
git push origin feature/github-actions-cicd
gh pr create --base develop
```

Done! 🚀

---

## What Gets Created

### Azure Resources (Auto-Created)
- ✅ Resource Group: `eshop-marketplace`
- ✅ Service Principal: `github-actions-eshop-marketplace`
- ✅ Azure Container Registry (ACR): `eshopmarketplaceacr.azurecr.io`
- ✅ Connection: GitHub Actions ↔ Azure authenticated

### GitHub Secrets (Auto-Set)
- `AZURE_CREDENTIALS` - Service principal JSON
- `AZURE_SUBSCRIPTION_ID` - Your subscription ID
- `AZURE_RESOURCE_GROUP` - `eshop-marketplace`
- `AZURE_LOCATION` - `eastus`
- `ACR_REGISTRY_NAME` - `eshopmarketplaceacr`
- `ACR_USERNAME` - ACR login username
- `ACR_PASSWORD` - ACR login password
- `LINEAR_API_KEY` - Your Linear token (manual)
- `LINEAR_BUILD_ISSUE_ID` - Your tracking issue (manual)
- `ASPIRE_MODULE_PATH` - Path to AppHost

### Files Created
- `.env.local` - Local secrets (git-ignored)
- `.env.example` - Template for .env.local

---

## CI/CD Pipeline Overview

The GitHub Actions workflow (`marketplace-cicd.yml`) does:

1. **Build & Test** - On every push/PR
   - Compile all services (.NET 10)
   - Run unit tests
   - Run functional tests
   - Report results

2. **Security Scan** - Vulnerability scanning
   - Trivy filesystem scan
   - Upload to GitHub Security tab

3. **Docker Images** - Build container images
   - 8 services (all APIs + processors)
   - Push to GitHub Container Registry

4. **Deploy to Staging** - When pushing to `develop`
   - Deploy via Aspire
   - Auto-provision Container Apps
   - 5-10 minute deployment

5. **Deploy to Production** - When pushing to `main`
   - Requires environment approval (in GitHub)
   - Same deployment as staging

6. **E2E Tests** - After staging deployment
   - Playwright tests on staging URL
   - Verify marketplace functionality

7. **Linear Integration** - Track build status
   - Comments on your Linear issue
   - Build pass/fail notifications

8. **Marketplace Features** - Special handling
   - Branches with `feature/marketplace` detected
   - Extra builds for VendorManagement, CommissionEngine, MarketplaceSearch APIs

---

## Deployment Workflow

```
Local Development
    ↓
Push to feature branch
    ↓
GitHub Actions triggers:
  1. Build & Test
  2. Security Scan
  3. Docker Images
  4. [if develop] Deploy to Staging → E2E Tests
  5. [if main] Deploy to Production
    ↓
Azure Container Apps
  - Staging: https://eshop-staging.azurecontainerapps.io
  - Production: https://eshop-marketplace.azurecontainerapps.io
```

---

## Common Commands

### After Setup

**View secrets in GitHub:**
```bash
gh secret list
```

**Monitor workflow:**
```bash
gh run list
gh run view {RUN_ID} --log
```

**View Azure resources:**
```bash
az group list --query "[].name"
az containerapp list --resource-group eshop-marketplace
```

**Check deployment status:**
```bash
az containerapp logs show \
  --name eshop-webapp \
  --resource-group eshop-marketplace \
  --follow
```

**Scale container app:**
```bash
az containerapp update \
  --name eshop-webapp \
  --resource-group eshop-marketplace \
  --min-replicas 2 \
  --max-replicas 10
```

---

## Troubleshooting

### Setup Script Issues

| Problem | Solution |
|---------|----------|
| "Azure CLI not found" | Install from https://learn.microsoft.com/en-us/cli/azure/install-azure-cli |
| "GitHub CLI not found" | Install from https://cli.github.com/ |
| "Permission denied" on .sh | Run: `chmod +x ./scripts/*.sh` |
| "Not authenticated" | Run: `az login` or `gh auth login` |

### Workflow Issues

| Problem | Solution |
|---------|----------|
| Tests fail | Check logs: `gh run view {ID} --log` |
| Docker build fails | Verify Dockerfile paths in workflow |
| Secrets not found | Verify in GitHub Settings → Secrets |
| Deployment timeout | Check Azure Portal → Container Apps logs |
| Port conflict | Check if service is already running |

### Azure Issues

| Problem | Solution |
|---------|----------|
| "Resource group exists" | Skip creation (normal - workflow handles it) |
| "Insufficient permissions" | Add Contributor role to service principal |
| "Container pull failed" | Check credentials in AZURE_CREDENTIALS |
| "Endpoint not found" | Verify resource is deployed in correct region |

---

## File Reference

| File | Purpose |
|------|---------|
| `.github/workflows/marketplace-cicd.yml` | Main CI/CD pipeline |
| `.env.example` | Template for .env.local |
| `.env.local` | Local secrets (git-ignored) |
| `.gitignore` | Excludes .env.local from commits |
| `scripts/setup-azure-and-github.sh` | Main setup (Bash) |
| `scripts/setup-azure-and-github.ps1` | Main setup (PowerShell) |
| `scripts/setup-github-secrets.sh` | Secrets sync (Bash) |
| `scripts/setup-github-secrets.ps1` | Secrets sync (PowerShell) |
| `docs/AZURE_SETUP.md` | Detailed Azure guide |
| `docs/GITHUB_ACTIONS_SECRETS.md` | All secrets reference |
| `docs/AZURE_CONTAINER_APPS_DEPLOYMENT.md` | Deployment details |
| `docs/AUTOMATED_SETUP_GUIDE.md` | Comprehensive setup guide |

---

## Environment Variables

### Azure
```bash
AZURE_SUBSCRIPTION_ID      # Your Azure subscription
AZURE_RESOURCE_GROUP       # eshop-marketplace
AZURE_LOCATION            # eastus
AZURE_CREDENTIALS         # Service principal JSON
```

### Foundry (Commented for now)
```bash
FOUNDRY_ENDPOINT          # Azure OpenAI endpoint
FOUNDRY_KEY              # Azure OpenAI API key
```

### Linear
```bash
LINEAR_API_KEY           # Your Linear token
LINEAR_API_ENDPOINT      # https://api.linear.app/graphql
LINEAR_BUILD_ISSUE_ID    # e.g., ESHOP-1
```

### Aspire
```bash
ASPIRE_MODULE_PATH       # src/eShop.AppHost/eShop.AppHost.csproj
```

---

## Next Steps After Setup

1. ✅ Run setup script
2. ✅ Verify secrets in GitHub
3. ✅ Push feature branch: `git push origin feature/github-actions-cicd`
4. ✅ Create PR: `gh pr create --base develop`
5. ✅ Monitor workflow: `gh run list`
6. ✅ Check deployment: Azure Portal → Container Apps
7. ✅ Test application: Visit deployed URL
8. ✅ Merge PR to `main` for production deployment

---

## Architecture Diagram

```
GitHub Repository
│
├─ Push to develop
│  └─ GitHub Actions Workflow
│     ├─ Build & Test (.NET, Tests)
│     ├─ Security Scan (Trivy)
│     ├─ Docker Images (8 services)
│     │  └─ Push to Azure Container Registry (ACR)
│     └─ Deploy to Staging
│        └─ Azure Container Apps (Staging Environment)
│           ├─ eshop-webapp
│           ├─ eshop-catalog-api
│           ├─ eshop-basket-api
│           ├─ eshop-ordering-api
│           ├─ eshop-identity-api
│           ├─ eshop-webhooks-api
│           ├─ eshop-order-processor
│           └─ eshop-payment-processor
│
└─ Push to main
   └─ GitHub Actions Workflow
      ├─ Build & Test
      ├─ Security Scan
      ├─ Docker Images (push to ACR)
      └─ Deploy to Production
         └─ Azure Container Apps (Production Environment)
            └─ [Same services as staging]
```

**Key Benefits of Using ACR:**
- ✅ Faster image pulls (same region as Container Apps)
- ✅ Private registry (not public)
- ✅ Integrated with Azure security
- ✅ Auto-webhook deployment triggers
- ✅ Built-in vulnerability scanning

---

## Cost Estimation

**Azure Container Apps (Monthly):**
- CPU: ~$40
- Memory: ~$5
- Log Analytics: ~$10
- Storage: ~$5
- **Total: ~$60/month** (dev tier)

**GitHub Actions:**
- Public repo: Free
- Private repo: 2,000 free minutes/month

---

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/)
- [GitHub Actions](https://docs.github.com/en/actions)
- [Linear API](https://developers.linear.app/)

---

## Support

For issues:
1. Check `docs/AUTOMATED_SETUP_GUIDE.md` for troubleshooting
2. Run: `az --help`, `gh --help` for CLI help
3. Check GitHub Actions logs: `gh run view {ID} --log`
4. Check Azure Portal for deployment status

---

**Ready to deploy? Run the setup script now!** 🚀

```bash
# Windows
.\scripts\setup-azure-and-github.ps1

# macOS/Linux
./scripts/setup-azure-and-github.sh
```
