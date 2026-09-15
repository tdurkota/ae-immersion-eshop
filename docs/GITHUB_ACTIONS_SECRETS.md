# GitHub Actions Secrets Configuration

This document explains all the secrets that need to be configured in your GitHub repository for the CI/CD pipeline.

## Setting Up Secrets

Go to: **Settings → Secrets and variables → Actions** → **New repository secret**

### Required Secrets

#### 1. Azure Deployment
```
AZURE_CREDENTIALS
- Type: Service Principal credentials (JSON format)
- How to create:
  az ad sp create-for-rbac --name "eshop-github-actions" \
    --role contributor \
    --scopes /subscriptions/{subscription-id} \
    --sdk-auth
- Paste the entire JSON output
```

```
AZURE_SUBSCRIPTION_ID
- Type: String
- Value: Your Azure subscription ID
- Find at: portal.azure.com → Subscriptions
```

```
AZURE_RESOURCE_GROUP
- Type: String
- Value: Name of your resource group (e.g., "eshop-marketplace")
```

```
AZURE_LOCATION
- Type: String
- Value: Azure region (e.g., "eastus", "westus2")
```

#### 2. Azure Foundry Integration (Chatbot)
```
FOUNDRY_ENDPOINT
- Type: String
- Value: Your Azure OpenAI/Foundry endpoint URL
- Find at: Azure Portal → Cognitive Services → Your instance → Endpoint
```

```
FOUNDRY_KEY
- Type: Secret
- Value: Your Azure OpenAI/Foundry API key
- Find at: Azure Portal → Cognitive Services → Your instance → Keys
⚠️ DO NOT commit this to the repository
```

#### 3. Linear Integration
```
LINEAR_API_KEY
- Type: Secret
- Value: Your Linear API token
- Get at: https://linear.app/settings/api → Create API key
```

```
LINEAR_API_ENDPOINT
- Type: String
- Value: Linear GraphQL API endpoint
- Default: "https://api.linear.app/graphql"
```

```
LINEAR_BUILD_ISSUE_ID
- Type: String
- Value: Issue ID in Linear to track builds
- Format: "MODULE-123" (or your workspace prefix)
```

#### 4. Container Registry (Optional - for private images)
```
REGISTRY_USERNAME
- Type: String
- Value: GitHub username (for GitHub Container Registry)
```

```
REGISTRY_PASSWORD
- Type: Secret
- Value: GitHub Personal Access Token (PAT)
  - Scopes needed: `write:packages`, `read:packages`
```

## Aspire Deployment Secrets

#### 5. Aspire Configuration
```
ASPIRE_MODULE_PATH
- Type: String
- Value: Path to your Aspire module
- Default: "src/eShop.AppHost/eShop.AppHost.csproj"
```

## Environment-Specific Secrets

### Staging Environment
- All secrets above
- `AZURE_RESOURCE_GROUP`: "eshop-marketplace-staging"
- `AZURE_LOCATION`: "eastus" (or your preference)

### Production Environment  
- All secrets above
- `AZURE_RESOURCE_GROUP`: "eshop-marketplace-prod"
- `AZURE_LOCATION`: "eastus" (or your preference)
- Set as Protected Branch Secret (Settings → Environments → production)

## Quick Setup Script

Save this as `setup-secrets.sh` and run it locally (NOT in CI):

```bash
#!/bin/bash

# Load from .env file (create this locally with sensitive values)
# .env should be in .gitignore

set -a
source .env
set +a

# Azure
gh secret set AZURE_CREDENTIALS --body "$(cat $AZURE_CREDS_FILE)"
gh secret set AZURE_SUBSCRIPTION_ID -b "$AZURE_SUBSCRIPTION_ID"
gh secret set AZURE_RESOURCE_GROUP -b "$AZURE_RESOURCE_GROUP"
gh secret set AZURE_LOCATION -b "$AZURE_LOCATION"

# Foundry
gh secret set FOUNDRY_ENDPOINT -b "$FOUNDRY_ENDPOINT"
gh secret set FOUNDRY_KEY -b "$FOUNDRY_KEY"

# Linear
gh secret set LINEAR_API_KEY -b "$LINEAR_API_KEY"
gh secret set LINEAR_API_ENDPOINT -b "$LINEAR_API_ENDPOINT"
gh secret set LINEAR_BUILD_ISSUE_ID -b "$LINEAR_BUILD_ISSUE_ID"

echo "✅ All secrets configured successfully!"
```

## Verification

After setting secrets, you can verify (list names only, not values):
```bash
gh secret list
```

## Security Best Practices

1. ✅ **Rotate regularly** - Especially FOUNDRY_KEY and AZURE_CREDENTIALS
2. ✅ **Use Azure Managed Identities** - For production, prefer managed identity over service principal
3. ✅ **Limit scope** - Service principal should only have necessary permissions
4. ✅ **Audit logs** - Monitor Azure Activity Log for secret usage
5. ✅ **Environment protection rules** - Enable approval for production deployments
6. ✅ **Never commit secrets** - Double-check `.gitignore` and use `git-secrets`

## Troubleshooting

### "Authentication failed"
- Verify Azure credentials JSON is valid
- Check subscription ID matches your Azure account
- Ensure service principal has Contributor role

### "Foundry endpoint not responding"
- Verify FOUNDRY_ENDPOINT URL is correct
- Check FOUNDRY_KEY has proper permissions
- Ensure resource is deployed in selected region

### "Linear API rate limit exceeded"
- Check LINEAR_API_KEY is active
- Reduce notification frequency
- Consider batching updates

## Related Documentation

- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
- [Azure Service Principal for GitHub Actions](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure)
- [Linear API Documentation](https://developers.linear.app/)
- [Azure Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
