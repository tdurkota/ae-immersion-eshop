# Production Rollout Plan: Third-Party Seller Marketplace

This guide provides the complete plan for launching the third-party seller marketplace to production with a phased approach: 5 test sellers → 24-hour monitoring → full launch.

## Overview

**Launch Strategy:** Gradual rollout with tight monitoring and immediate rollback capability

**Timeline:**
- **Phase 1 (Day 1, Hours 0-2):** Deploy to production with seller registration disabled
- **Phase 2 (Day 1, Hours 2-4):** Enable registration for 5 test sellers only
- **Phase 3 (Day 1-2, Hours 4-28):** Monitor intensively for issues, 24-hour observation period
- **Phase 4 (Day 2, Hour 28+):** Enable full seller registration for all users (if no critical issues)

## Pre-Production Final Checks

### 1. Security Audit Completion

- [ ] Code review approved by security team
- [ ] No SQL injection vulnerabilities
- [ ] No authorization bypasses
- [ ] JWT validation tested
- [ ] Rate limiting configured on registration endpoint
- [ ] Input validation verified
- [ ] Secrets are not logged or exposed

### 2. Performance Baseline

```bash
# Run baseline performance tests
npm run perf-test:load

# Expected results (P95 latency):
# - Seller registration: < 200ms
# - Get seller profile: < 100ms
# - List payouts: < 300ms
# - Create order: < 500ms
# Database:
# - 95th percentile query time: < 100ms
# - Connection pool utilization: < 70%
```

### 3. Monitoring Setup Verification

- [ ] Application Insights configured
- [ ] Dashboards created (Seller Metrics, Orders, Payouts, Errors)
- [ ] Alerts configured for:
  - [ ] Error rate > 1%
  - [ ] API latency P95 > 1 second
  - [ ] Database connection pool > 80%
  - [ ] Payout reconciliation failures
  - [ ] Authorization failures (possible attack)
  - [ ] Service unavailability
- [ ] On-call team briefed
- [ ] War room/incident channel established
- [ ] Rollback plan reviewed and approved

### 4. Production Database Backup

```bash
# Create backup immediately before deployment
az postgres flexible-server backup create \
  --resource-group rg-eshop-prod \
  --server-name prod-postgresql \
  --backup-name pre-seller-marketplace-launch-$(date +%Y%m%d_%H%M%S)

# Verify backup
az postgres flexible-server backup list \
  --resource-group rg-eshop-prod \
  --server-name prod-postgresql | head -5
```

### 5. Communications Plan

- [ ] Status page message drafted
- [ ] Customer support briefed on new feature
- [ ] Escalation contacts verified
- [ ] External communication plan (if launching seller program publicly)

---

## Phase 1: Deployment (Hours 0-2)

### 1. Deploy Services to Production

```bash
# Ensure all services ready
az containerapp list --resource-group rg-eshop-prod

# Deploy in order (each with health check pause)
# 1. Identity.API (seller role support)
az containerapp update \
  --resource-group rg-eshop-prod \
  --name identity-api \
  --image eshopprod.azurecr.io/identity-api:$VERSION

# Wait 2 minutes, verify healthy
sleep 120
az containerapp logs show --resource-group rg-eshop-prod --name identity-api --tail 20

# 2. Sellers.API (new service)
az containerapp update \
  --resource-group rg-eshop-prod \
  --name sellers-api \
  --image eshopprod.azurecr.io/sellers-api:$VERSION

sleep 120
az containerapp logs show --resource-group rg-eshop-prod --name sellers-api --tail 20

# 3. Catalog.API (seller product support)
az containerapp update \
  --resource-group rg-eshop-prod \
  --name catalog-api \
  --image eshopprod.azurecr.io/catalog-api:$VERSION

sleep 120
az containerapp logs show --resource-group rg-eshop-prod --name catalog-api --tail 20

# 4. Ordering.API (commission tracking)
az containerapp update \
  --resource-group rg-eshop-prod \
  --name ordering-api \
  --image eshopprod.azurecr.io/ordering-api:$VERSION

sleep 120
az containerapp logs show --resource-group rg-eshop-prod --name ordering-api --tail 20
```

### 2. Apply Database Migrations

```bash
# Set production connection string
export PROD_DB_CONN="postgresql://dbadmin:$DB_PASSWORD@prod-db.postgres.database.azure.com:5432/eshop"

# Apply migrations to each service
cd src/Sellers.API
dotnet ef database update --configuration Release

cd ../Catalog.API
dotnet ef database update --configuration Release

cd ../Ordering.API
dotnet ef database update --configuration Release

# Verify migrations in production
psql $PROD_DB_CONN -c "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 10;"
```

### 3. Verify Production Services

```bash
# Health check all services
for service in sellers-api catalog-api ordering-api identity-api; do
  echo "Checking $service..."
  az containerapp logs show --resource-group rg-eshop-prod --name $service --tail 5
done

# Check application insights for errors
az monitor metrics list \
  --resource-group rg-eshop-prod \
  --resource-type Microsoft.App/containerApps \
  --metric-names Requests,RequestsFailedCount \
  --interval PT5M \
  --max-results 10
```

Expected: All services running, no errors in logs.

### 4. Smoke Tests (Production)

```bash
# Basic connectivity
curl -i https://prod-sellers-api.azurecontainers.io/health
curl -i https://prod-identity-api.azurecontainers.io/health
curl -i https://prod-catalog-api.azurecontainers.io/health

# Verify existing functionality still works (customer purchase flow)
# This uses the existing product catalog and order system

# 1. List products (should include any seller products, but registration disabled yet)
curl https://prod-catalog-api.azurecontainers.io/api/v1/catalog/products

# 2. Place order with existing platform products
# This verifies ordering system still works

echo "Phase 1 Complete: Services deployed and running"
```

Expected: All endpoints return HTTP 200 OK.

---

## Phase 2: Enable Test Seller Registration (Hours 2-4)

### 1. Create 5 Test Sellers

Create 5 test seller accounts using the API. These are real accounts that will be monitored closely.

```bash
#!/bin/bash
# Create 5 test sellers
TEST_SELLERS=(
  '{"name":"Test Seller A","email":"test-seller-a-'$(date +%s)'@example.com","description":"QA test seller","commissionRate":0.15}'
  '{"name":"Test Seller B","email":"test-seller-b-'$(date +%s)'@example.com","description":"QA test seller","commissionRate":0.15}'
  '{"name":"Test Seller C","email":"test-seller-c-'$(date +%s)'@example.com","description":"QA test seller","commissionRate":0.15}'
  '{"name":"Test Seller D","email":"test-seller-d-'$(date +%s)'@example.com","description":"QA test seller","commissionRate":0.15}'
  '{"name":"Test Seller E","email":"test-seller-e-'$(date +%s)'@example.com","description":"QA test seller","commissionRate":0.15}'
)

for seller_json in "${TEST_SELLERS[@]}"; do
  seller_id=$(curl -s -X POST https://prod-sellers-api.azurecontainers.io/api/sellers \
    -H "Content-Type: application/json" \
    -d "$seller_json" | jq -r '.sellerId')
  echo "Created seller: $seller_id"
  echo "$seller_id" >> test-sellers-list.txt
done

# Save seller list for reference
echo "Test sellers created and saved to test-sellers-list.txt"
```

### 2. Test Seller Workflow

```bash
# For each test seller:
# 1. Login
# 2. Add a product
# 3. Verify product in catalog
# 4. Purchase as test customer
# 5. Verify payout entry created

# Example for Test Seller A:
SELLER_EMAIL="test-seller-a-TIMESTAMP@example.com"
SELLER_PASSWORD="TestSellerPass123!"

# Login
TOKEN=$(curl -s -X POST https://prod-identity-api.azurecontainers.io/api/identity/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$SELLER_EMAIL\",\"password\":\"$SELLER_PASSWORD\"}" | jq -r '.accessToken')

# Add product
PRODUCT=$(curl -s -X POST https://prod-catalog-api.azurecontainers.io/api/sellers/$SELLER_ID/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Test Product",
    "description":"Quality test product",
    "price":29.99,
    "stock":100,
    "pictureUrl":"https://example.com/product.jpg"
  }' | jq -r '.productId')

echo "Test seller workflow completed successfully"
```

### 3. Start Monitoring

```bash
# Open monitoring dashboards (Application Insights)
az monitor metrics list \
  --resource-group rg-eshop-prod \
  --resource-type Microsoft.App/containerApps \
  --metric-names Requests,RequestsFailedCount,Cpu,MemoryUsage \
  --interval PT1M \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%SZ)

# Start streaming logs for each service
az containerapp logs show --resource-group rg-eshop-prod --name sellers-api --follow &
az containerapp logs show --resource-group rg-eshop-prod --name ordering-api --follow &
az containerapp logs show --resource-group rg-eshop-prod --name catalog-api --follow &
```

### 4. Verification

- [ ] All 5 test sellers registered successfully
- [ ] Each seller can login and receive JWT token with seller_id claim
- [ ] Each seller can create products
- [ ] Products appear in catalog with seller attribution
- [ ] Customers can purchase seller products
- [ ] Payout entries created for seller orders
- [ ] No errors in application logs
- [ ] API response times normal (< 500ms P95)

---

## Phase 3: 24-Hour Monitoring Period

### Hour-by-Hour Monitoring Tasks

**Hour 0-1:**
- [ ] Monitor error rate (should be ~0%)
- [ ] Monitor API latency (P95 should be < 500ms)
- [ ] Check database connection pool utilization (should be < 70%)
- [ ] Verify payout entries created for all test orders
- [ ] Check RabbitMQ queue depths (should process within 1 second)

**Hour 1-2:**
- [ ] Review authorization logs (should have no 403 errors from system bugs)
- [ ] Verify seller isolation (no cross-seller data access)
- [ ] Check data consistency (orders ↔ payouts reconciliation)
- [ ] Monitor for duplicate data or race conditions

**Hour 2-4:**
- [ ] Performance remains stable under baseline load
- [ ] No database deadlocks or timeouts
- [ ] Event processing latency consistent
- [ ] Customer support receives no Seller-related issues

**Hour 4-8:**
- [ ] Generate interim payout report (verify calculations)
- [ ] Monitor for any data integrity issues
- [ ] Verify scaling behavior if traffic increases
- [ ] Check database backup completion

**Hour 8-12:**
- [ ] Deep analysis of metrics from first half
- [ ] Query performance audit
- [ ] Review any warnings/errors in logs
- [ ] Test admin functions (viewing payouts, generating reports)

**Hour 12-24:**
- [ ] Continued monitoring
- [ ] Generate comprehensive 24-hour report
- [ ] Final verification of all test seller workflows
- [ ] Prepare full launch recommendation

### Critical Metrics to Watch

```sql
-- Query 1: Verify Payout Ledger Consistency
SELECT 
  COUNT(*) as payout_count,
  SUM(gross_amount) as total_gross,
  SUM(seller_amount) as total_seller,
  SUM(commission_amount) as total_commission
FROM seller_payout
WHERE created_at > NOW() - INTERVAL '24 hours';

-- Query 2: Verify Commission Calculation (should balance: seller + commission = gross)
SELECT 
  COUNT(CASE WHEN (seller_amount + commission_amount) = gross_amount THEN 1 END) as valid_rows,
  COUNT(*) as total_rows,
  COUNT(CASE WHEN (seller_amount + commission_amount) <> gross_amount THEN 1 END) as invalid_rows
FROM seller_payout
WHERE created_at > NOW() - INTERVAL '24 hours';

-- Query 3: Order-Payout Reconciliation
SELECT 
  COUNT(DISTINCT order_id) as unique_orders,
  COUNT(DISTINCT ole.id) as order_line_items,
  COUNT(sp.payout_id) as payout_entries
FROM ordering.order_line_item ole
LEFT JOIN sellers.seller_payout sp ON ole.id = sp.order_line_item_id
WHERE ole.seller_id IS NOT NULL
  AND ole.created_at > NOW() - INTERVAL '24 hours';

-- Query 4: Seller Data Integrity
SELECT 
  COUNT(*) as total_sellers,
  COUNT(CASE WHEN status = 'Active' THEN 1 END) as active,
  COUNT(CASE WHEN status = 'Suspended' THEN 1 END) as suspended
FROM sellers.seller
WHERE created_at > NOW() - INTERVAL '24 hours';
```

### Application Insights Queries

```kusto
// Query 1: Error Rate
requests
| where timestamp > ago(24h)
| summarize FailureRate = (todouble(sum(itemCount) where success == false) / sum(itemCount)) * 100
  by bin(timestamp, 1h)
| render timechart

// Query 2: Seller API Latency
requests
| where name contains "seller" and timestamp > ago(24h)
| summarize P95_Latency = percentile(duration, 95), Count = count()
  by bin(timestamp, 1h)
| render timechart

// Query 3: Authorization Failures
requests
| where resultCode == 403 and timestamp > ago(24h)
| summarize Count = count() by name, resultCode
| render table

// Query 4: Exception Analysis
exceptions
| where timestamp > ago(24h)
| summarize Count = count() by type, message
| render table
```

### Monitoring Report Template

Create a report tracking:
- Total transactions processed
- Number of seller registrations
- Number of products listed
- Number of orders with seller products
- Payout entries created and verified
- API error rate (target: < 0.5%)
- API latency P95 (target: < 500ms)
- Data integrity issues (target: 0)
- Support escalations related to sellers (target: 0)
- Critical issues discovered: [none expected]

### Decision Criteria for Full Launch

**PROCEED TO FULL LAUNCH if:**
- [ ] Error rate < 0.5%
- [ ] P95 API latency < 500ms
- [ ] No data integrity issues detected
- [ ] Reconciliation discrepancies = 0
- [ ] Authorization working correctly (no unauthorized access)
- [ ] All payout entries created correctly
- [ ] No customer complaints or support issues
- [ ] Database performance stable
- [ ] RabbitMQ processing events reliably

**PAUSE BEFORE FULL LAUNCH if:**
- [ ] Error rate > 2%
- [ ] P95 latency > 1 second
- [ ] Any data integrity issues found
- [ ] Reconciliation discrepancies detected
- [ ] Authorization bypass discovered
- [ ] Missing payout entries
- [ ] Performance degradation trend
- [ ] Service unavailability > 5 minutes

---

## Phase 4: Full Launch (Hour 28+)

### 1. Final Pre-Launch Checks

```bash
# Verify 24-hour monitoring report is complete
# Confirm all success criteria met
# Get approval from product/engineering leads
# Brief support team on new feature
```

### 2. Enable Full Seller Registration

```bash
# Enable seller registration for all users
# This may involve:
# - Removing feature flag limiting test sellers
# - Opening seller sign-up on UI
# - Sending announcement email to existing users

# Option 1: Feature flag (if implemented)
az keyvault secret set \
  --vault-name eshop-keyvault-prod \
  --name "FeatureFlags:AllowSellerRegistration" \
  --value "true"

# Option 2: UI configuration
# Update web app to show seller registration link

# Option 3: Email announcement
# Send "Become a Seller" email to customer list
```

### 3. Continued Monitoring

After enabling full registration, continue monitoring for first 48 hours:
- Seller registration rate
- API performance under full load
- Database performance
- Error rates

```bash
# Monitor registration spike
az monitor metrics list \
  --resource-group rg-eshop-prod \
  --resource-type Microsoft.App/containerApps \
  --metric-names Requests \
  --interval PT1M \
  --start-time $(date -u +%Y-%m-%dT%H:%M:%SZ) \
  --end-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%SZ)

# Track seller growth
psql $PROD_DB_CONN -c "
  SELECT 
    DATE(created_at) as registration_date,
    COUNT(*) as new_sellers
  FROM sellers.seller
  WHERE created_at > NOW() - INTERVAL '7 days'
  GROUP BY DATE(created_at)
  ORDER BY registration_date DESC;
"
```

### 4. Success Criteria for Full Launch

- [ ] 100+ new sellers registered within first 24 hours
- [ ] Error rate remains < 1%
- [ ] API performance stable under load
- [ ] No data integrity issues
- [ ] Customer support requests manageable
- [ ] Revenue from seller products tracking correctly
- [ ] Payout processing timely

---

## Rollback Plan

### If Critical Issues Discovered

**Immediate Actions:**
1. Declare incident
2. Alert on-call team
3. Assess severity
4. Initiate rollback if needed

### Partial Rollback (Stop New Registrations)

```bash
# Disable seller registration without affecting existing sellers
# Option 1: Feature flag
az keyvault secret set \
  --vault-name eshop-keyvault-prod \
  --name "FeatureFlags:AllowSellerRegistration" \
  --value "false"

# Option 2: Revert UI change (hide registration)
# Update web app configuration

# Existing sellers continue operating
# New registrations temporarily paused
# Issue can be investigated and fixed
```

### Full Rollback (Restore from Backup)

```bash
# Only if data corruption or critical failure
# Contact platform team for coordinated rollback

# 1. Alert all teams
# 2. Pause all new operations
# 3. Restore database from backup
# 4. Restart services
# 5. Run smoke tests
# 6. Post-incident review

# Estimated time to restore: 30-60 minutes
```

---

## Post-Launch (Day 3+)

### First Week Monitoring
- [ ] Daily payout reconciliation report
- [ ] Daily error rate report
- [ ] Monitor for seller abuse or compliance issues
- [ ] Customer support metrics
- [ ] Revenue tracking

### First Month Tasks
- [ ] Analyze seller demographics and behavior
- [ ] Identify top-performing sellers
- [ ] Review commission rate optimization
- [ ] Plan Phase 2 features based on usage
- [ ] Seller onboarding experience improvements

---

## Contacts & Escalation

**On-Call Engineer:** [Contact info]
**Product Manager:** [Contact info]
**Platform Team:** [Contact info]
**Customer Support Lead:** [Contact info]
**CEO/Leadership:** [Contact info]

## Appendix: Scripts

### Quick Health Check Script

```bash
#!/bin/bash
# Quick health check for production deployment

echo "=== Production Seller Marketplace Health Check ==="
echo "Time: $(date)"
echo ""

echo "1. Service Status:"
for service in sellers-api catalog-api ordering-api identity-api; do
  status=$(az containerapp show --resource-group rg-eshop-prod --name $service --query "properties.runningStatus" -o tsv)
  echo "  $service: $status"
done

echo ""
echo "2. Recent Errors (last 10):"
az containerapp logs show --resource-group rg-eshop-prod --name sellers-api --tail 10 | grep -i error

echo ""
echo "3. Payout Reconciliation:"
psql $PROD_DB_CONN -c "
  SELECT 
    COUNT(*) as total_payouts,
    SUM(CASE WHEN (seller_amount + commission_amount) = gross_amount THEN 1 END) as valid,
    COUNT(*) - SUM(CASE WHEN (seller_amount + commission_amount) = gross_amount THEN 1 END) as invalid
  FROM sellers.seller_payout
  WHERE created_at > NOW() - INTERVAL '1 hour';
"

echo ""
echo "=== Health Check Complete ==="
```

Use this script hourly during Phase 3 and immediately after Phase 4 launch.
