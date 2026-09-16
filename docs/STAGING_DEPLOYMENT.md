# Staging Deployment Guide

This guide provides step-by-step instructions for deploying the third-party seller marketplace to the staging environment.

## Prerequisites

1. **Access & Permissions**
   - Access to Azure subscription (staging resource group)
   - Access to staging database
   - Docker/container registry access
   - GitHub repository access (for deploying from branches)

2. **Environment**
   - Local machine with Aspire CLI installed: `aspire --version`
   - .NET 10 SDK: `dotnet --version`
   - Container runtime running (Docker or Podman)
   - GitHub CLI: `gh --version` (optional but recommended)

3. **Staging Infrastructure**
   - Staging resource group created
   - Staging PostgreSQL database ready
   - Staging RabbitMQ instance ready
   - Container registry configured
   - DNS/networking prepared

## Pre-Deployment Validation (Local)

### 1. Verify Code Quality

```bash
# Clone and checkout the section-13-documentation-deployment branch
git clone https://github.com/dotnet/eShop.git
cd eShop
git checkout section-13-documentation-deployment

# Run all tests
dotnet test eShop.Web.slnf --logger "console;verbosity=minimal"

# Verify specific seller marketplace tests
dotnet test tests/ --filter "Seller|Commission|Payout" --verbosity normal
```

Expected: All tests pass with no warnings.

### 2. Verify Build Success

```bash
# Clean build
dotnet clean
dotnet build eShop.Web.slnf /WarnAsError

# Build individual services
dotnet build src/Sellers.API -c Release /WarnAsError
dotnet build src/Catalog.API -c Release /WarnAsError
dotnet build src/Ordering.API -c Release /WarnAsError
dotnet build src/Identity.API -c Release /WarnAsError
```

Expected: Zero errors, zero warnings.

### 3. Verify Migrations

```bash
# List pending migrations for each service
dotnet ef migrations list --project src/Sellers.API
dotnet ef migrations list --project src/Catalog.API
dotnet ef migrations list --project src/Ordering.API

# Verify migrations are well-formed (dry-run)
dotnet ef migrations list --project src/Sellers.API --detailed
```

Expected: Migration names include version and description (e.g., `20260916190415_InitialSellerSchema`).

### 4. Local End-to-End Test

```bash
# Start Aspire locally
aspire run

# In another terminal, run smoke tests
npm run test:e2e:staging

# Test workflow:
# 1. Register seller
# 2. Log in as seller
# 3. Add product
# 4. Purchase as customer
# 5. Verify payout entry created
```

Expected: All E2E tests pass with no errors.

---

## Staging Database Preparation

### 1. Connect to Staging Database

```bash
# Using psql
psql -h staging-db.postgres.database.azure.com \
     -U dbadmin \
     -d eshop \
     -p 5432

# Or using Azure CLI
az postgres flexible-server connect \
  --resource-group rg-eshop-staging \
  --name staging-postgresql
```

### 2. Create Database Backup

```bash
# Create full backup
az postgres flexible-server backup create \
  --resource-group rg-eshop-staging \
  --server-name staging-postgresql \
  --backup-name pre-seller-marketplace-$(date +%Y%m%d_%H%M%S)

# Verify backup
az postgres flexible-server backup list \
  --resource-group rg-eshop-staging \
  --server-name staging-postgresql
```

### 3. Apply Migrations to Staging

```bash
# Set staging connection string
export ConnectionStrings__eshopdb="Host=staging-db.postgres.database.azure.com;Username=dbadmin;Password=$STAGING_DB_PASSWORD;Database=eshop;Port=5432"

# Apply migrations
cd src/Sellers.API
dotnet ef database update

cd ../Catalog.API
dotnet ef database update

cd ../Ordering.API
dotnet ef database update

# Verify migrations applied
psql -h staging-db.postgres.database.azure.com -U dbadmin -d eshop -c \
  "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 5;"
```

Expected: Migration entries appear in `__EFMigrationsHistory` table.

### 4. Verify Schema Changes

```sql
-- Verify Seller table exists
SELECT column_name, data_type FROM information_schema.columns 
WHERE table_name='seller' ORDER BY ordinal_position;

-- Verify SellerId added to CatalogItem
SELECT column_name, data_type FROM information_schema.columns 
WHERE table_name='catalog_item' AND column_name='seller_id';

-- Verify SellerId and CommissionRate added to OrderLineItem
SELECT column_name, data_type FROM information_schema.columns 
WHERE table_name='order_line_item' 
AND column_name IN ('seller_id', 'commission_rate') 
ORDER BY ordinal_position;

-- Verify SellerPayout table
SELECT column_name, data_type FROM information_schema.columns 
WHERE table_name='seller_payout' ORDER BY ordinal_position;

-- Verify indexes
SELECT indexname FROM pg_indexes WHERE tablename = 'seller_payout';
```

Expected: All expected columns exist with correct data types.

---

## Build and Publish

### 1. Build Docker Images

```bash
# Build all images for staging
docker build -f src/Sellers.API/Dockerfile \
  -t eshop-sellers-api:staging-$(date +%Y%m%d.%H%M%S) .

docker build -f src/Catalog.API/Dockerfile \
  -t eshop-catalog-api:staging-$(date +%Y%m%d.%H%M%S) .

docker build -f src/Ordering.API/Dockerfile \
  -t eshop-ordering-api:staging-$(date +%Y%m%d.%H%M%S) .

docker build -f src/Identity.API/Dockerfile \
  -t eshop-identity-api:staging-$(date +%Y%m%d.%H%M%S) .
```

### 2. Push to Container Registry

```bash
# Login to registry
az acr login --name eshopstaging

# Tag and push
export TAG=$(date +%Y%m%d.%H%M%S)
docker tag eshop-sellers-api:staging-$TAG eshopstaging.azurecr.io/sellers-api:$TAG
docker push eshopstaging.azurecr.io/sellers-api:$TAG

docker tag eshop-catalog-api:staging-$TAG eshopstaging.azurecr.io/catalog-api:$TAG
docker push eshopstaging.azurecr.io/catalog-api:$TAG

docker tag eshop-ordering-api:staging-$TAG eshopstaging.azurecr.io/ordering-api:$TAG
docker push eshopstaging.azurecr.io/ordering-api:$TAG

docker tag eshop-identity-api:staging-$TAG eshopstaging.azurecr.io/identity-api:$TAG
docker push eshopstaging.azurecr.io/identity-api:$TAG
```

### 3. Or: Use Aspire Deployment Pipeline

```bash
# Publish using Aspire (generates all artifacts)
aspire publish --output-path ./publish-staging

# Or deploy directly
aspire deploy --environment staging
```

---

## Deploy to Staging

### Option 1: Using Aspire Deploy

```bash
# Deploy with Aspire
aspire deploy \
  --environment staging \
  --location eastus \
  --resource-group rg-eshop-staging

# Monitor deployment
aspire ps --environment staging
```

### Option 2: Manual Azure Container Apps Deployment

```bash
# Create container apps environment (if not exists)
az containerapp env create \
  --resource-group rg-eshop-staging \
  --name eshop-staging-env \
  --location eastus

# Update each container app
az containerapp update \
  --resource-group rg-eshop-staging \
  --name sellers-api \
  --image eshopstaging.azurecr.io/sellers-api:$TAG \
  --set-env-vars ConnectionStrings__eshopdb=@storage_connstr \
                    ConnectionStrings__rabbitmq=@rabbitmq_connstr

# Similar for catalog-api, ordering-api, identity-api

# Restart services for clean startup
az containerapp restart \
  --resource-group rg-eshop-staging \
  --name sellers-api

az containerapp restart \
  --resource-group rg-eshop-staging \
  --name identity-api

az containerapp restart \
  --resource-group rg-eshop-staging \
  --name catalog-api

az containerapp restart \
  --resource-group rg-eshop-staging \
  --name ordering-api
```

### Option 3: Using GitHub Actions

If configured in `.github/workflows/deploy-staging.yml`:

```bash
# Trigger deployment workflow
gh workflow run deploy-staging.yml \
  --ref section-13-documentation-deployment \
  -f environment=staging
```

---

## Post-Deployment Health Checks

### 1. Verify Services Are Running

```bash
# Check container status
az containerapp show \
  --resource-group rg-eshop-staging \
  --name sellers-api \
  --query "{status: properties.runningStatus, latestRevision: properties.latestRevisionName}"

# View recent logs (last 50 lines)
az containerapp logs show \
  --resource-group rg-eshop-staging \
  --name sellers-api \
  --tail 50

# Similar for other services
az containerapp logs show --resource-group rg-eshop-staging --name identity-api --tail 50
az containerapp logs show --resource-group rg-eshop-staging --name catalog-api --tail 50
az containerapp logs show --resource-group rg-eshop-staging --name ordering-api --tail 50
```

Expected: Services show status "Running" with no error messages in logs.

### 2. Verify API Accessibility

```bash
# Get service URLs
az containerapp show \
  --resource-group rg-eshop-staging \
  --name sellers-api \
  --query "properties.configuration.ingress.fqdn"

# Test endpoint
SELLERS_API_URL=$(az containerapp show --resource-group rg-eshop-staging --name sellers-api --query 'properties.configuration.ingress.fqdn' -o tsv)
curl -i https://$SELLERS_API_URL/health
```

Expected: HTTP 200 OK response.

### 3. Test Basic Functionality

```bash
# Set staging URLs
export IDENTITY_URL="https://identity-api-staging.azurecontainers.io"
export SELLERS_URL="https://sellers-api-staging.azurecontainers.io"
export CATALOG_URL="https://catalog-api-staging.azurecontainers.io"

# Test 1: Seller Registration
curl -X POST $SELLERS_URL/api/sellers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Staging Test Seller",
    "email": "staging-test-'$(date +%s)'@example.com",
    "description": "Staging test",
    "commissionRate": 0.15
  }'
# Expect: 201 Created with sellerId

# Test 2: Authentication
curl -X POST $IDENTITY_URL/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"username":"alice","password":"Pass123$"}'
# Expect: 200 OK with JWT token

# Test 3: Get Seller Profile
curl -X GET $SELLERS_URL/api/sellers/<sellerId> \
  -H "Authorization: Bearer <token>"
# Expect: 200 OK with seller details
```

---

## Run Full E2E Test Suite

### 1. Prepare Test Environment

```bash
# Update .env file for staging
cp .env.example .env
# Edit .env with staging URLs and credentials:
# STAGING_IDENTITY_URL=https://identity-api-staging.azurecontainers.io
# STAGING_SELLERS_URL=https://sellers-api-staging.azurecontainers.io
# STAGING_CATALOG_URL=https://catalog-api-staging.azurecontainers.io
# STAGING_ORDERING_URL=https://ordering-api-staging.azurecontainers.io
# TEST_SELLER_EMAIL=staging-seller-$(date +%s)@example.com
# TEST_SELLER_PASSWORD=TestPass123!
```

### 2. Run E2E Tests

```bash
# Install dependencies
npm ci

# Run all E2E tests against staging
npm run test:e2e:staging

# Or specific test suites
npm run test:e2e:staging -- --grep "seller"
npm run test:e2e:staging -- --grep "commission"
npm run test:e2e:staging -- --grep "payout"

# Generate test report
npm run test:e2e:staging -- --reporter html
# Report available at: test-results/index.html
```

### 3. Key Test Scenarios

```bash
# Test 1: Complete Seller Workflow
# 1. Register seller
# 2. Login as seller
# 3. Add product
# 4. Verify product appears in catalog
# 5. Purchase product as customer
# 6. Verify order created
# 7. Verify payout entry created for seller
# 8. Seller views order and payout

# Test 2: Multi-Seller Order
# 1. Register 3 sellers
# 2. Each seller adds 2 products
# 3. Customer orders from all 3 sellers (1-2 items each)
# 4. Verify single order created with multiple line items
# 5. Verify each seller sees their items
# 6. Verify 3 payout entries created (one per seller)

# Test 3: Seller Isolation & Authorization
# 1. Register 2 sellers
# 2. Seller A attempts to view Seller B's data
# 3. Verify 403 Forbidden response
# 4. Verify no data leakage

# Test 4: Payout Tracking
# 1. Create multiple orders from same seller
# 2. Query payout ledger
# 3. Verify all orders have corresponding payout entries
# 4. Verify summary totals match ledger entries
# 5. Test filtering by date range and status
```

### 4. Performance Testing

```bash
# Run load test (example with k6)
npm run load-test:staging

# Monitor metrics during test
az monitor metrics list \
  --resource-group rg-eshop-staging \
  --resource-type Microsoft.App/containerApps \
  --resource-namespace Microsoft.App/containerApps \
  --metric-names Cpu --interval PT1M
```

Expected: API responds within 500ms P95 latency under load.

---

## Verification Checklist

- [ ] All services running (status: Running)
- [ ] No error messages in logs
- [ ] Seller registration works
- [ ] Authentication returns valid JWT with seller_id claim
- [ ] Seller profile endpoint accessible
- [ ] Products can be created with seller_id
- [ ] Orders created with seller attribution
- [ ] Payout entries automatically created
- [ ] Multi-seller orders handled correctly
- [ ] Seller isolation verified (no unauthorized access)
- [ ] E2E tests pass
- [ ] Performance acceptable (< 500ms P95)
- [ ] Database queries performant
- [ ] RabbitMQ processing events without errors

## Troubleshooting

### Services Not Starting

```bash
# Check logs for startup errors
az containerapp logs show --resource-group rg-eshop-staging --name sellers-api --follow

# Check if environment variables are set
az containerapp show --resource-group rg-eshop-staging --name sellers-api \
  --query "properties.template.containers[0].env"

# Verify database connectivity
psql -h staging-db.postgres.database.azure.com \
     -U dbadmin -d eshop \
     -c "SELECT 1"
```

### Database Connection Errors

```bash
# Verify connection string format
az containerapp show --resource-group rg-eshop-staging --name sellers-api \
  --query "properties.template.containers[0].env[?name=='ConnectionStrings__eshopdb'].value"

# Test connection manually
psql "postgresql://dbadmin:password@staging-db.postgres.database.azure.com:5432/eshop"
```

### Event Processing Not Working

```bash
# Check RabbitMQ connectivity
az containerapp logs show --resource-group rg-eshop-staging --name sellers-api --follow | grep -i rabbit

# Verify queues exist
# (Would need RabbitMQ management console access)

# Check dead-letter queue for failed messages
# (Configure dead-letter monitoring)
```

## Next Steps

After successful staging deployment:
1. Share staging URL with stakeholders for review
2. Conduct any additional UAT testing
3. Prepare production deployment plan
4. Schedule production deployment window
5. Brief incident response team
6. Proceed to production deployment (see PRODUCTION_ROLLOUT.md)
