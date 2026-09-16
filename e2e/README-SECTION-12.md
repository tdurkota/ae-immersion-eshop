# Section 12: Testing & Validation - Playwright E2E Tests

This directory contains comprehensive Playwright end-to-end tests for the third-party seller marketplace feature.

## Test Files

### 12.1 - Complete Marketplace Flow (seller-e2e-flow.spec.ts)
Tests the entire end-to-end flow:
- Seller registration via API
- Seller login
- Product addition
- Customer search for product
- Customer purchase
- Seller order visibility
- Payout ledger creation

**Run:**
```bash
npx playwright test seller-e2e-flow.spec.ts
```

### 12.2 - Seller Signup Flow (seller-signup.spec.ts)
Tests seller registration and profile setup:
- Valid seller registration with all fields
- Seller profile setup navigation
- Product listing page access
- Email format validation
- Duplicate email rejection
- Required field validation

**Run:**
```bash
npx playwright test seller-signup.spec.ts
```

### 12.3 - Customer Finding & Ordering (customer-seller-ordering.spec.ts)
Tests customer search, filtering, and purchasing:
- Search for seller products by name
- Seller attribution display on listings
- Seller attribution on product detail pages
- Filter products by seller
- Add seller product to cart and checkout
- Order attribution to correct seller
- Commission display on confirmation

**Run:**
```bash
npx playwright test customer-seller-ordering.spec.ts
```

### 12.4 - Performance Tests (performance.spec.ts)
Tests catalog query performance with target <200ms latency:
- Load catalog page within 200ms
- Search catalog with large product sets
- Filter by seller within 200ms
- Search by keyword within 200ms
- Pagination performance
- Get seller details within 200ms
- Load seller products within 200ms

**Run:**
```bash
npx playwright test performance.spec.ts --reporter=html
```

Performance metrics are printed to console and detailed results saved in HTML report.

### 12.5 - Manual Spot-Check (spot-check.ts)
Utility script for manual testing with commission calculation verification:
- Creates 3 test sellers with different commission rates
- Adds 10 products across sellers
- Places 5 test orders
- Verifies commission calculations
- Validates reconciliation

**Run:**
```bash
npx ts-node e2e/spot-check.ts
```

### 12.6 - Authorization & Security (security-authorization.spec.ts)
Tests authorization and security controls:
- Reject access to another seller's profile (403)
- Prevent private field exposure
- Reject profile modification by other sellers (403)
- Reject cross-seller order access (403)
- Reject cross-seller payout access (403)
- Prevent seller deletion by others (403)
- Ensure data isolation between sellers
- Prevent data leakage in error messages
- Validate authorization token claims
- Prevent seller isolation bypass
- Reject unauthenticated access
- Prevent cross-seller order visibility

**Run:**
```bash
npx playwright test security-authorization.spec.ts
```

## Quick Start

### Prerequisites
```bash
npm install
```

### Run All Seller Marketplace Tests
```bash
npx playwright test --grep "Seller|Performance|Authorization"
```

### Run Specific Test Group
```bash
# E2E flow tests only
npx playwright test seller-e2e-flow.spec.ts

# Signup tests only
npx playwright test seller-signup.spec.ts

# Security tests only
npx playwright test security-authorization.spec.ts
```

### Run with UI Mode (for debugging)
```bash
npx playwright test --ui
```

### Run with Debug Mode
```bash
npx playwright test --debug
```

## Environment Variables

Set these in your `.env` file for test execution:

```bash
# Application URL
PLAYWRIGHT_BASE_URL=http://localhost:5045

# Test user credentials
USERNAME1=testuser
PASSWORD=Test@123

# Seller test tokens (for API tests)
TEST_SELLER_TOKEN=your-seller-token
TEST_SELLER_1_TOKEN=seller1-token
TEST_SELLER_2_TOKEN=seller2-token
TEST_SELLER_PASSWORD=Test@123
```

## Test Results

Results are saved to:
- HTML Report: `playwright-report/index.html`
- Test Results: `test-results/`

Open the HTML report:
```bash
npx playwright show-report
```

## Performance Baseline

Expected performance metrics (<200ms):
- Catalog page load: ~50-150ms
- Catalog search: ~30-100ms
- Filter by seller: ~20-80ms
- Get seller details: ~10-50ms
- Load seller products: ~20-100ms

If queries exceed 200ms:
1. Check database indexes on seller_id, product_name, category
2. Verify N+1 query issues
3. Review paging/limit implementation
4. Check for missing query optimization

## Security Test Validation

All authorization tests verify:
- ✓ 403 responses for unauthorized access
- ✓ No cross-seller data visibility
- ✓ Private fields not exposed
- ✓ Error messages don't leak system info
- ✓ Token claim validation
- ✓ Seller isolation enforcement

## Commission Calculation Verification

The spot-check utility verifies:
- Commission rates match seller configuration
- Order totals calculate correctly
- Payout amounts = order total × commission rate
- Reconciliation matches between orders and payouts
- No calculation rounding errors (±$0.01)

## Continuous Integration

These tests are configured to run in CI with:
- 2 retries on failure
- HTML report generation
- Trace collection on first retry
- Single worker (serial execution due to shared state)

## Troubleshooting

### Tests timeout
- Ensure app is running: `dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj`
- Check BASE_URL is correct
- Increase timeout: `test.setTimeout(60000)`

### Authorization tests fail
- Verify seller_id claims in JWT tokens
- Check token has required claims
- Ensure authorization middleware is enabled

### Performance tests exceed 200ms
- Run in isolation: `npx playwright test performance.spec.ts`
- Check for network latency
- Profile database queries
- Verify indexes are created

### Spot-check fails
- Ensure app is running
- Verify database is initialized
- Check seller commission rates are set correctly
- Review order totals in database

## Notes

- Tests use unique timestamps in data to avoid conflicts
- Tests clean up created data when possible
- Some tests require app to be running with seed data
- Performance tests assume database has reasonable test data
- Security tests verify both API and UI access controls
