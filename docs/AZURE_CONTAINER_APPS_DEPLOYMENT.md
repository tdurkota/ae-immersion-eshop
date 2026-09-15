# Azure Container Apps Deployment Guide

This guide explains what's automated in the GitHub Actions workflow and what requires manual setup on Azure.

## Automated vs Manual

### ✅ AUTOMATED (by `aspire publish` command)

The GitHub Actions workflow automates:

1. **Build & Test** - Compile and test all services
2. **Docker Images** - Build container images for each service
3. **Container Registry** - Push images to GitHub Container Registry (GHCR)
4. **Container Apps Provisioning** - `aspire publish --provision-if-not-exists`
   - Creates Container Apps environment (if doesn't exist)
   - Deploys container apps for each service
   - Configures networking between services
   - Sets up environment variables
   - Configures ingress/endpoints
5. **Service Dependencies** - Wires up database connections, cache, message bus

### ⚠️ MANUAL SETUP (must do before first deployment)

Before the GitHub Actions workflow can deploy, you need to manually set up on Azure:

1. **Resource Group** - Create it with Azure CLI or Portal
   ```bash
   az group create --name eshop-marketplace --location eastus
   ```

2. **Service Principal** - For GitHub Actions to authenticate
   ```bash
   az ad sp create-for-rbac \
     --name "github-actions-eshop-marketplace" \
     --role contributor \
     --scopes /subscriptions/{SUBSCRIPTION_ID} \
     --sdk-auth
   ```

3. **Container Registry** (Optional but recommended for production)
   - Can use GitHub Container Registry (included in workflow)
   - Or create Azure Container Registry for better integration

4. **Database Servers** (Optional - Aspire can provision via containers)
   - PostgreSQL can run in containers (default)
   - Or use Azure Database for PostgreSQL

5. **Message Queue** (Optional - Aspire can provision via containers)
   - RabbitMQ can run in containers (default)
   - Or use Azure Service Bus

6. **Storage Account** (If needed for logs/monitoring)

## Detailed Setup Process

### Step 1: Create Resource Group (Manual)

```bash
az group create \
  --name eshop-marketplace \
  --location eastus
```

**Save:**
- Resource Group Name: `eshop-marketplace`
- Location: `eastus`

### Step 2: Create Service Principal (Manual)

```bash
az ad sp create-for-rbac \
  --name "github-actions-eshop-marketplace" \
  --role contributor \
  --scopes /subscriptions/{YOUR_SUBSCRIPTION_ID} \
  --sdk-auth
```

**Save the JSON output** - this is your `AZURE_CREDENTIALS` secret

### Step 3: Set GitHub Secrets (Manual)

See [GITHUB_ACTIONS_SECRETS.md](GITHUB_ACTIONS_SECRETS.md) for details.

Key secrets for deployment:
```
AZURE_CREDENTIALS={JSON from Step 2}
AZURE_SUBSCRIPTION_ID={your subscription ID}
AZURE_RESOURCE_GROUP=eshop-marketplace
AZURE_LOCATION=eastus
```

### Step 4: First Deployment (Automated)

Once secrets are configured, create a PR to `develop` branch:

```bash
# Create feature branch
git checkout -b feature/first-deployment

# Make a small change to trigger workflow
echo "# Deploying to staging" >> README.md

# Push and create PR
git push origin feature/first-deployment
```

**The workflow will:**
1. ✅ Build all services
2. ✅ Run tests
3. ✅ Create Docker images
4. ✅ Deploy to staging:
   - Create Container Apps environment (auto-created)
   - Deploy all microservices
   - Configure networking
   - Set up load balancing
   - Configure DNS

### Step 5: Monitor First Deployment

Watch the GitHub Actions logs:

```bash
# List workflow runs
gh run list

# Watch specific run
gh run view {RUN_ID} --log
```

Or check Azure Portal:
- Container Apps → Environment → eshop-staging
- Monitor resource creation progress

## What Gets Created Automatically on Azure

When `aspire publish` runs for the first time, it creates:

```
Resource Group: eshop-marketplace
├── Container Apps Environment: eshop-staging
│   ├── Container App: eshop-basket-api
│   ├── Container App: eshop-catalog-api
│   ├── Container App: eshop-identity-api
│   ├── Container App: eshop-ordering-api
│   ├── Container App: eshop-webapp
│   ├── Container App: eshop-order-processor
│   ├── Container App: eshop-payment-processor
│   ├── Container App: eshop-webhooks-api
│   ├── Container App: eshop-redis (cache)
│   ├── Container App: eshop-postgres (database)
│   └── Container App: eshop-rabbitmq (message bus)
├── Managed Environment
│   ├── Log Analytics Workspace
│   ├── Container Registry (GitHub owned)
│   └── Networking configuration
└── Secrets/Credentials
    └── Database passwords
    └── Connection strings
    └── API keys (managed automatically)
```

## Subsequent Deployments

After the first deployment, subsequent pushes to `develop` or `main` will:

1. Trigger the workflow automatically
2. Skip resource creation (already exists)
3. Update/redeploy container apps with new images
4. Zero downtime deployments (rolling updates)

## Monitoring & Troubleshooting

### View Logs

```bash
# View Container App logs
az containerapp logs show \
  --name eshop-webapp \
  --resource-group eshop-marketplace \
  --follow

# View in Azure Portal
# Container Apps → eshop-webapp → Logs
```

### Check Deployment Status

```bash
# List all container apps
az containerapp list \
  --resource-group eshop-marketplace

# Check specific app status
az containerapp show \
  --name eshop-webapp \
  --resource-group eshop-marketplace \
  --query "properties.runningStatus"
```

### Common Issues

#### "Resource group does not exist"
**Fix:** Create it manually first
```bash
az group create --name eshop-marketplace --location eastus
```

#### "Insufficient permissions"
**Fix:** Check service principal has Contributor role
```bash
az role assignment list --all \
  --query "[?principalName=='github-actions-eshop-marketplace']"
```

#### "Deployment timeout"
**Fix:** Container Apps takes 5-10 minutes for first deployment
- Check logs: `az containerapp logs show --name {app} --resource-group eshop-marketplace`
- Check Container App status in Portal

#### "Image pull failed"
**Fix:** Ensure GitHub credentials are correct
- Check `AZURE_CREDENTIALS` secret is valid JSON
- Verify service principal has access to resource group

## Customization After Deployment

After initial deployment, you can customize via:

### Azure Portal
- Adjust CPU/memory per service
- Scale up/down replicas
- Configure auto-scaling rules
- Add custom domains/SSL
- Set up monitoring alerts

### Azure CLI
```bash
# Scale a container app
az containerapp update \
  --name eshop-webapp \
  --resource-group eshop-marketplace \
  --min-replicas 2 \
  --max-replicas 10

# Add environment variable
az containerapp update \
  --name eshop-webapp \
  --resource-group eshop-marketplace \
  --set-env-vars MY_VAR=value
```

### Via Aspire (Re-deploy workflow)
Simply update `aspire.config.json` or AppHost and push → workflow redeploys

## Cost Estimation

Azure Container Apps pricing (approximate for eshop-marketplace):

| Component | Cost/Month | Notes |
|-----------|-----------|-------|
| Container Apps CPU | $40 | 1 vCPU shared across services |
| Container Apps Memory | $5 | 2GB shared |
| Log Analytics | $10 | 5GB ingestion/month |
| Storage (logs) | $5 | Archive after 30 days |
| **Total** | **~$60** | Development tier |

Production with auto-scaling could be $100-300/month depending on traffic.

## Next Steps

1. ✅ Create Resource Group
2. ✅ Create Service Principal
3. ✅ Set GitHub Secrets
4. ✅ Push first feature branch to trigger workflow
5. ✅ Monitor deployment
6. ✅ Test deployed application
7. ✅ Create PR and merge to deploy to staging
8. ✅ Merge develop → main for production deployment
