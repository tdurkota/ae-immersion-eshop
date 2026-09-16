# Simplified CI/CD Workflow (MVP)

This document describes the current simplified GitHub Actions workflow optimized for initial deployment.

## Current Pipeline (Simplified)

```
┌─────────────────┐
│  Code Push      │ (main or develop branch)
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 1. Build & Test                     │
│   - dotnet restore                  │
│   - dotnet build (Release)          │
│   ✓ Tests commented (TODO)          │
└────────┬────────────────────────────┘
         │
         ▼ (if push event)
┌─────────────────────────────────────┐
│ 2. Build Docker Images (Parallel)   │
│   - 8 services (matrix)             │
│   - Build & push to ACR             │
│   ✓ Cache disabled (for simplicity) │
└────────┬────────────────────────────┘
         │
    ┌────┴─────────────┐
    │                  │
    ▼ (develop)       ▼ (main)
┌─────────────┐  ┌──────────────┐
│  Staging    │  │  Production  │
│  Deploy     │  │  Deploy      │
└─────────────┘  └──────────────┘
    ↓                ↓
Azure Container Apps Staging & Production
```

## What's Enabled

✅ **build-and-test** job
- Dotnet restore & build only
- Runs on all pushes to main/develop
- Fast compilation check

✅ **build-docker-images** job
- Builds 8 services in parallel (matrix strategy)
- Pushes to Azure Container Registry (ACR)
- Uses ACR credentials from GitHub secrets
- Metadata tagging (branch, SHA)

✅ **deploy-staging** job
- Triggered on `develop` branch pushes
- Uses `aspire publish` for Container Apps deployment
- Auto-provisions resources with `--provision-if-not-exists`
- Environment URL: `https://eshop-staging.azurecontainerapps.io`

✅ **deploy-production** job
- Triggered on `main` branch pushes
- Concurrency lock to prevent simultaneous deployments
- Same Aspire deployment as staging
- Environment URL: `https://eshop-marketplace.azurecontainerapps.io`

## What's Commented (TODO)

🔵 **Tests** (in build-and-test)
- Unit tests commented
- Functional tests commented
- Test result publishing commented
- **Reason:** Keep initial build fast, add after MVP works

🔵 **security-scan** job
- Trivy vulnerability scanning commented
- SARIF upload commented
- **Reason:** Add security layer after MVP deployment confirmed

🔵 **e2e-tests** job
- Playwright E2E tests commented
- Depends on staging deployment
- **Reason:** Enable after staging environment is stable

🔵 **notify-linear** job
- Linear integration commented
- Build notifications to Linear commented
- **Reason:** Add after core workflow proven

🔵 **build-marketplace-features** job
- Marketplace-specific service builds commented
- VendorManagement.API, CommissionEngine.API, MarketplaceSearch.API
- **Reason:** Enable when implementing marketplace features

## Trigger Events

The workflow runs on:

```yaml
on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
```

**No path filters** - runs on all code changes to keep it simple.

## Secrets Required

For this simplified workflow, you need:

| Secret | Purpose |
|--------|---------|
| `ACR_REGISTRY_NAME` | Azure Container Registry name |
| `ACR_USERNAME` | ACR login username |
| `ACR_PASSWORD` | ACR login password |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_CREDENTIALS` | Service principal JSON |
| `AZURE_RESOURCE_GROUP` | Resource group name |
| `AZURE_LOCATION` | Azure region |
| `ASPIRE_MODULE_PATH` | Path to AppHost project |

Set these with the automation script:
```bash
./scripts/setup-azure-and-github.sh  # Linux/macOS
.\scripts\setup-azure-and-github.ps1  # Windows
```

## Next Steps to Enable Features

### Phase 1: MVP (Current)
- ✅ Build existing code
- ✅ Push to ACR
- ✅ Deploy to Azure Container Apps

### Phase 2: Testing
- 🔵 Uncomment unit tests in `build-and-test` job
- 🔵 Uncomment functional tests
- 🔵 Add test result publishing

### Phase 3: Security
- 🔵 Enable `security-scan` job
- 🔵 Add Trivy vulnerability scanning
- 🔵 Configure SARIF reporting

### Phase 4: E2E Testing
- 🔵 Enable `e2e-tests` job
- 🔵 Add Playwright automation tests
- 🔵 Smoke test deployments

### Phase 5: Marketplace Features
- 🔵 Enable `build-marketplace-features` job
- 🔵 Add marketplace microservices
- 🔵 Implement vendor management, commissions, etc.

### Phase 6: Monitoring & Notifications
- 🔵 Enable `notify-linear` job
- 🔵 Add deployment status notifications
- 🔵 Track builds in Linear project management

## Manual Testing

To test the workflow without a full push:

```bash
# 1. Make a small code change
echo "# Test" >> README.md

# 2. Commit and push to feature branch
git checkout -b test/workflow-check
git add .
git commit -m "test: workflow validation"
git push origin test/workflow-check

# 3. Create PR to develop
gh pr create --base develop --head test/workflow-check

# 4. Monitor workflow
gh run list
gh run view {RUN_ID} --log

# 5. Delete branch when done
git push origin --delete test/workflow-check
gh pr close {PR_NUMBER}
```

## Monitoring Deployments

### View workflow runs:
```bash
gh run list
gh run view {ID} --log
```

### View Azure resources:
```bash
az containerapp list --resource-group eshop-marketplace
az containerapp logs show --name eshop-webapp --resource-group eshop-marketplace
```

### Visit deployed apps:
- **Staging:** https://eshop-staging.azurecontainerapps.io
- **Production:** https://eshop-marketplace.azurecontainerapps.io

## Troubleshooting

### Build fails
```bash
# Check log output
gh run view {ID} --log | grep -i error

# Test locally
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Docker push fails
```bash
# Verify ACR credentials
az acr login --name eshopmarketplaceacr

# Check secrets
gh secret list | grep -i acr
```

### Deployment times out
```bash
# Check Aspire module path
echo $ASPIRE_MODULE_PATH

# Verify resource group exists
az group show --name eshop-marketplace

# Check container app status
az containerapp show --name eshop-webapp --resource-group eshop-marketplace
```

## Performance Notes

**Current MVP characteristics:**
- 🟢 Fast builds (no tests = ~3-5 min)
- 🟢 Parallel Docker builds (8 services simultaneously)
- 🟢 Simple deployment (no rollback strategy yet)
- 🔴 No caching (fresh builds each time - OK for MVP)
- 🔴 No security scanning (add in Phase 3)
- 🔴 No E2E validation (add in Phase 4)

**Estimated time per workflow run:** ~10-15 minutes total

---

## Quick Reference: Enabling Features

### Enable Unit Tests
Edit `.github/workflows/marketplace-cicd.yml`:
1. Find `# TODO: Enable tests after initial deployment is working`
2. Uncomment the `# - name: Run unit tests` section
3. Commit and push

### Enable E2E Tests
Edit `.github/workflows/marketplace-cicd.yml`:
1. Find `# TODO: Enable E2E tests after initial deployment is working`
2. Uncomment the entire `# e2e-tests:` job
3. Commit and push

### Enable Security Scanning
Edit `.github/workflows/marketplace-cicd.yml`:
1. Find `# TODO: Enable security scanning after initial deployment is working`
2. Uncomment the entire `# security-scan:` job
3. Add back `security-scan` to deploy job dependencies
4. Commit and push

---

**Status:** Production-ready for MVP deployments. All complex features documented for gradual enablement.
