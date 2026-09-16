# Aspire Automatic ACR Provisioning

## Overview

When using **Aspire Deploy**, Azure Container Registry (ACR) is **automatically created and managed** as part of the infrastructure provisioning process. You do not need to manually create or configure an ACR.

## Why Manual ACR Setup is Not Needed

### Previous Approach (Removed)
The initial setup scripts included a manual step to create an Azure Container Registry:
```powershell
az acr create --resource-group $resourceGroupName --name $acrName --sku Standard
```

### Current Approach (Aspire-Driven)
Aspire automatically provisions all required infrastructure, including ACR, during deployment:
- **Automatic registry creation**: When you run `aspire deploy`, Aspire creates an ACR with auto-generated naming
- **Auto-generated naming**: ACR names follow the pattern `aca[randomString].azurecr.io`
- **Managed credentials**: Aspire handles registry access credentials internally
- **No manual secrets needed**: ACR credentials don't need to be stored in GitHub secrets

## What Actually Happens

When you deploy with Aspire:

1. **AppHost defines resources** - `src/eShop.AppHost/Program.cs` includes:
   ```csharp
   builder.AddAzureContainerAppEnvironment("aca");
   ```

2. **Aspire provisions infrastructure** - Running `aspire deploy` automatically:
   - Creates Azure Container Registry
   - Builds and pushes container images
   - Deploys to Azure Container Apps
   - Configures networking and authentication

3. **Registry is auto-managed** - No manual intervention needed:
   ```
   Auto-generated ACR: acaacrq67ufwprwwax6.azurecr.io
   Created images:     webapp, catalog-api, basket-api, ordering-api, etc.
   ```

## GitHub Secrets Now Needed

After the refactor, **only Azure authentication secrets are required**:

```
✅ AZURE_CREDENTIALS         - Service Principal JSON
✅ AZURE_SUBSCRIPTION_ID     - Your subscription ID
✅ AZURE_RESOURCE_GROUP      - Resource group name
✅ AZURE_LOCATION            - Azure region
✅ ASPIRE_MODULE_PATH        - Path to AppHost project
```

**No longer needed:**
```
❌ ACR_REGISTRY_NAME
❌ ACR_USERNAME
❌ ACR_PASSWORD
```

## .env.local Configuration

The `.env.local` file no longer includes ACR credentials:

```dotenv
# AZURE CONFIGURATION
AZURE_CREDENTIALS='{...}'
AZURE_SUBSCRIPTION_ID='0ed46a28-9584-4008-90e1-6b2f1c215dff'
AZURE_RESOURCE_GROUP='eshop-marketplace'
AZURE_LOCATION='westeurope'

# Aspire handles ACR automatically - no manual config needed

# ASPIRE CONFIGURATION
ASPIRE_MODULE_PATH='src/eShop.AppHost/eShop.AppHost.csproj'
```

## How to Deploy

1. **Run setup script** (creates resource group and service principal):
   ```bash
   ./scripts/setup-azure-and-github.ps1  # Windows
   ./scripts/setup-azure-and-github.sh   # macOS/Linux
   ```

2. **ACR is created automatically** during first deployment via GitHub Actions workflow

3. **View deployed apps**:
   ```bash
   az containerapp list --resource-group eshop-marketplace
   ```

## Verification

After deployment, verify your auto-created ACR:

```bash
# List all container registries
az acr list --query "[].{name:name, loginServer:loginServer}"

# Output example:
# [
#   {
#     "name": "acaacrq67ufwprwwax6",
#     "loginServer": "acaacrq67ufwprwwax6.azurecr.io"
#   }
# ]
```

## Key Takeaway

**Aspire eliminates the need for manual infrastructure provisioning.** The setup scripts now focus solely on Azure authentication (Service Principal), while Aspire handles all container infrastructure including ACR creation, image building, and deployment.

This reduces setup complexity and keeps your infrastructure definition in code (via the AppHost) rather than scattered across manual scripts.
