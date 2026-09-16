# Deployment Checklist: Third-Party Seller Marketplace

This checklist ensures all components are properly deployed and configured for the third-party seller marketplace feature.

## Pre-Deployment: Development Environment

### Database Setup
- [ ] PostgreSQL database connection verified
- [ ] Aspire AppHost configured with PostgreSQL dependency
- [ ] All migrations created and applied:
  - [ ] Section 1: Seller and SellerPayout tables
  - [ ] Section 1: SellerId added to CatalogItem
  - [ ] Section 4: Seller schema with constraints
  - [ ] Section 7: SellerId and CommissionRate added to OrderLineItem
- [ ] Run `dotnet ef database update` in each service
- [ ] Verify schema with: `SELECT * FROM information_schema.tables WHERE table_schema='public'`

### Identity.API Configuration
- [ ] Seller role added to role enum
- [ ] seller_id claim added to JWT token generation
- [ ] Test authentication flow:
  ```bash
  curl -X POST https://localhost:5000/api/identity/login \
    -H "Content-Type: application/json" \
    -d '{"username":"alice","password":"Pass123$"}'
  ```
- [ ] Verify JWT token includes `seller_id` claim (if seller role)
- [ ] Verify JWT token includes `role: "seller"` claim

### Sellers.API Setup
- [ ] New Sellers.API project created in `src/Sellers.API`
- [ ] ServiceDefaults integration applied
- [ ] PostgreSQL database context configured (`AddNpgsqlDbContext<SellersContext>`)
- [ ] Controllers implemented:
  - [ ] POST /api/sellers (seller registration)
  - [ ] GET /api/sellers/{id} (seller profile)
  - [ ] PUT /api/sellers/{id} (update profile)
  - [ ] GET /api/sellers/{id}/payouts (payout ledger)
  - [ ] GET /api/sellers/{id}/payouts/summary (payout summary)
- [ ] Build succeeds: `dotnet build src/Sellers.API/Sellers.API.csproj`

### Catalog.API Integration
- [ ] SellerId (GUID, nullable) field added to CatalogItem entity
- [ ] Migration applied for seller_id column
- [ ] Index created on seller_id for query performance
- [ ] Product responses include seller attribution:
  - [ ] sellerId field in product DTO
  - [ ] sellerName field in product DTO
  - [ ] GET /api/products returns seller info
- [ ] New endpoints implemented:
  - [ ] POST /api/sellers/{id}/products (seller adds product)
  - [ ] PUT /api/sellers/{id}/products/{productId} (seller updates product)
  - [ ] DELETE /api/sellers/{id}/products/{productId} (seller deletes product)
  - [ ] GET /api/sellers/{id}/products (seller views own products)
  - [ ] GET /api/sellers/{id}/storefront/products (public seller storefront)
- [ ] Seller authorization checks in place

### Ordering.API Integration
- [ ] SellerId and CommissionRate fields added to OrderLineItem
- [ ] Migration applied for new columns
- [ ] CommissionService implemented with unit tests
- [ ] OrderService updated to:
  - [ ] Capture seller_id at order time
  - [ ] Calculate and store commission_rate on line item
  - [ ] Calculate commission_amount and seller_amount
  - [ ] Create SellerPayout entries on order completion
- [ ] Order response DTOs include seller and commission info
- [ ] OrderCreatedIntegrationEvent includes seller data

### RabbitMQ Event Integration
- [ ] RabbitMQ service running and accessible
- [ ] OrderCreatedIntegrationEvent definition includes:
  - [ ] SellerId field
  - [ ] CommissionRate field
  - [ ] LineItem seller details
- [ ] Sellers.API event handler implemented:
  - [ ] `IIntegrationEventHandler<OrderCreatedIntegrationEvent>`
  - [ ] Creates SellerPayout entries on event receipt
  - [ ] Handles multi-seller orders correctly
  - [ ] Retry logic for transient failures
- [ ] Dead-letter queue configured for failed events

### API Documentation
- [ ] Swagger/OpenAPI enabled in Sellers.API
- [ ] All endpoints documented with:
  - [ ] Summary and description
  - [ ] Request/response examples
  - [ ] Produces/Consumes content types
  - [ ] ProducesResponseType for each status code
  - [ ] Authorization requirements noted
- [ ] Verify Swagger UI: `https://localhost:5001/swagger/ui`

### Testing
- [ ] Unit tests pass for all new components:
  ```bash
  dotnet test tests/ --filter "Seller|Commission|Payout"
  ```
- [ ] Integration tests verify event processing
- [ ] Authorization tests verify seller isolation
- [ ] Manual test flow:
  1. Register seller
  2. Add product as seller
  3. Purchase product as customer
  4. Verify payout entry created
  5. Verify seller sees order and payout

---

## Staging Deployment

### Pre-Deployment Validation

#### Environment Preparation
- [ ] Staging database backed up
- [ ] Staging PostgreSQL upgraded (if needed)
- [ ] Container registry accessible
- [ ] Azure Container Apps environment verified
- [ ] DNS records point to staging URLs
- [ ] SSL certificates valid for staging domain

#### Code Quality Checks
- [ ] All tests pass locally: `dotnet test eShop.Web.slnf`
- [ ] Code review approved
- [ ] No compiler warnings in Sellers.API: `dotnet build /WarnAsError`
- [ ] Static analysis clean (if configured)

### Deployment Steps

#### 1. Build and Publish
- [ ] Run build pipeline for staging:
  ```bash
  dotnet publish src/Sellers.API -c Release -o ./publish/sellers-api
  dotnet publish src/Catalog.API -c Release -o ./publish/catalog-api
  dotnet publish src/Ordering.API -c Release -o ./publish/ordering-api
  dotnet publish src/Identity.API -c Release -o ./publish/identity-api
  ```

#### 2. Database Migration
- [ ] Create backup of staging database
- [ ] Apply pending migrations:
  ```bash
  dotnet ef database update --context SellersContext --startup-project src/Sellers.API
  dotnet ef database update --context CatalogContext --startup-project src/Catalog.API
  dotnet ef database update --context OrderingContext --startup-project src/Ordering.API
  ```
- [ ] Verify migration success: Check `__EFMigrationsHistory` table
- [ ] Verify schema changes with queries:
  ```sql
  SELECT column_name FROM information_schema.columns WHERE table_name='seller';
  SELECT column_name FROM information_schema.columns WHERE table_name='order_line_item';
  SELECT column_name FROM information_schema.columns WHERE table_name='catalog_item';
  ```

#### 3. Service Deployment (Aspire)
- [ ] Deploy AppHost to staging:
  ```bash
  aspire deploy --environment staging
  ```
- [ ] Or manual deployment steps:
  1. Push images to container registry
  2. Deploy to Azure Container Apps
  3. Update environment variables
  4. Restart services in order:
     - [ ] Identity.API
     - [ ] Sellers.API
     - [ ] Catalog.API
     - [ ] Ordering.API
     - [ ] API Gateway / Reverse Proxy

#### 4. Configuration Validation
- [ ] Environment variables set correctly:
  - [ ] Database connection strings
  - [ ] RabbitMQ connection string
  - [ ] JWT signing key
  - [ ] Swagger UI enabled (staging only)
- [ ] Service-to-service communication working
- [ ] RabbitMQ queues and exchanges created

### Post-Deployment Validation

#### Health Checks
- [ ] All services report healthy in Aspire dashboard
- [ ] Database connectivity verified for each service
- [ ] RabbitMQ connection successful
- [ ] Event processing logs show no errors

#### Smoke Tests
- [ ] **Seller Registration:**
  ```bash
  curl -X POST https://staging.example.com/api/sellers \
    -H "Content-Type: application/json" \
    -d '{"name":"Test Seller","email":"test@example.com","commissionRate":0.15}'
  # Expect: 201 Created
  ```

- [ ] **Seller Authentication:**
  ```bash
  curl -X POST https://staging.example.com/api/identity/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test@example.com","password":"password"}'
  # Expect: 200 OK with JWT token including seller_id claim
  ```

- [ ] **Get Seller Profile:**
  ```bash
  curl -X GET https://staging.example.com/api/sellers/{sellerId} \
    -H "Authorization: Bearer <token>"
  # Expect: 200 OK with seller details
  ```

- [ ] **Add Product:**
  ```bash
  curl -X POST https://staging.example.com/api/sellers/{sellerId}/products \
    -H "Authorization: Bearer <seller-token>" \
    -d '{"name":"Test Product","price":10.00,...}'
  # Expect: 201 Created
  ```

- [ ] **Multi-Seller Order:**
  1. Create 2 test sellers
  2. Add products from each seller
  3. Customer orders products from both sellers
  4. Verify single order created with multiple line items
  5. Verify each seller sees their items in order
  6. Verify payout entries created for both sellers

- [ ] **Payout Tracking:**
  ```bash
  curl -X GET https://staging.example.com/api/sellers/{sellerId}/payouts \
    -H "Authorization: Bearer <seller-token>"
  # Expect: 200 OK with payout ledger
  ```

#### Logging and Monitoring
- [ ] Application Insights/logging showing events flowing through
- [ ] No error messages in service logs
- [ ] Event processing latency < 1 second (order → payout entry)
- [ ] Database query performance acceptable
- [ ] RabbitMQ queue depths reasonable (< 100 messages)

#### Admin Functions
- [ ] Admin API endpoints accessible with admin token
- [ ] `GET /api/admin/sellers` returns all sellers
- [ ] `GET /api/admin/payouts` returns all payouts
- [ ] `GET /api/admin/payouts/report` generates report
- [ ] Suspend/reinstate functionality works

---

## Production Deployment

### Pre-Production Checklist

#### Security
- [ ] All secrets rotated (JWT key, DB password, RabbitMQ credentials)
- [ ] Production database encrypted at rest
- [ ] SSL/TLS certificates valid and auto-renewal configured
- [ ] API keys rotated (if using key-based auth)
- [ ] Swagger UI disabled in production
- [ ] Authorization checks verified in code review
- [ ] SQL injection risks assessed and mitigated
- [ ] Rate limiting configured on public endpoints

#### Scalability & Reliability
- [ ] Load testing completed with 100+ concurrent sellers
- [ ] Database indexes verified for payout queries
- [ ] Connection pooling configured (max pool size: 20-30)
- [ ] RabbitMQ replicated for high availability
- [ ] Service replicas/instances configured (min 2)
- [ ] Auto-scaling policies defined based on CPU/memory
- [ ] Circuit breakers configured for service calls
- [ ] Retry policies configured with exponential backoff

#### Backup & Disaster Recovery
- [ ] Database backup schedule configured (daily, automated)
- [ ] Backup retention policy (30 days minimum)
- [ ] Disaster recovery plan tested (restore from backup)
- [ ] Transaction logs backed up (if applicable)
- [ ] Backup verification process automated

#### Monitoring & Alerting
- [ ] Application Insights configured
- [ ] Key metrics dashboards created:
  - [ ] Seller count (new registrations, suspended)
  - [ ] Order count and gross sales
  - [ ] Payout ledger (pending, processed, paid)
  - [ ] Service availability and latency
- [ ] Alerts configured for:
  - [ ] High payout pending amount
  - [ ] Service errors or high latency
  - [ ] Database connection failures
  - [ ] Reconciliation discrepancies
- [ ] On-call runbook prepared
- [ ] Escalation procedures documented

#### Documentation
- [ ] Deployment runbook finalized
- [ ] Configuration documented (all environment variables)
- [ ] Service dependencies mapped
- [ ] Rollback procedure documented and tested
- [ ] Admin guide reviewed and published
- [ ] API documentation (Swagger specs) exported

### Production Deployment Steps

#### 1. Pre-Deployment Communication
- [ ] Customer support briefed on new feature
- [ ] Incident response team on standby
- [ ] Monitoring dashboards open
- [ ] Deployment window communicated (if scheduled)

#### 2. Database Preparation
- [ ] Production database backup completed
- [ ] Backup verified restorable
- [ ] Apply migrations (with pre-deployment testing)
- [ ] Verify schema changes

#### 3. Service Deployment
- [ ] Deploy services in dependency order:
  1. Identity.API (updated with seller role)
  2. Sellers.API (new service)
  3. Catalog.API (seller attribution)
  4. Ordering.API (commission tracking)
- [ ] Verify each service healthy before next
- [ ] Monitor logs for errors during rollout

#### 4. Gradual Rollout
- [ ] **Phase 1 (Hour 1):** 20% of traffic to new seller features
  - [ ] Monitor error rates
  - [ ] Monitor latency
  - [ ] Check payout ledger creation
- [ ] **Phase 2 (Hour 2):** 50% of traffic
  - [ ] Continue monitoring
  - [ ] Verify seller registrations working
- [ ] **Phase 3 (Hour 3):** 100% of traffic
  - [ ] Monitor for 24 hours

#### 5. Smoke Tests (Production)
- [ ] Run full smoke test suite
- [ ] Verify Swagger endpoints accessible (behind auth if protected)
- [ ] Test seller registration with real data
- [ ] Test seller login and JWT token generation
- [ ] Complete end-to-end transaction with seller product

### Post-Production Monitoring (First 24 Hours)

#### Metrics to Watch
- [ ] Error rate < 0.5%
- [ ] P95 latency < 500ms for API endpoints
- [ ] Payout entries created immediately after order completion
- [ ] No reconciliation discrepancies
- [ ] Database performance stable
- [ ] RabbitMQ queue depths normal

#### Daily Monitoring Tasks
- [ ] Review error logs for anomalies
- [ ] Check reconciliation report
- [ ] Monitor seller sign-up rate
- [ ] Monitor payout processing
- [ ] Validate authorization (no unauthorized access)
- [ ] Check performance dashboard trends

#### Rollback Criteria
- [ ] Critical security vulnerability discovered
- [ ] Error rate exceeds 5%
- [ ] Data corruption detected (reconciliation failure)
- [ ] Service unavailability > 5 minutes
- [ ] Cascading failures across services

**Rollback Process:**
1. Notify stakeholders
2. Stop accepting new seller registrations (optional flag)
3. Revert database migrations (if applicable)
4. Redeploy previous service versions
5. Verify rollback success with smoke tests
6. Post-incident review and analysis

---

## Ongoing Operations

### Weekly Tasks
- [ ] Review payout reports for accuracy
- [ ] Check seller suspension/compliance metrics
- [ ] Monitor for any data discrepancies
- [ ] Review customer support tickets related to sellers

### Monthly Tasks
- [ ] Performance analysis (query times, throughput)
- [ ] Database maintenance (index analysis, stats update)
- [ ] Review and update runbooks as needed
- [ ] Capacity planning (storage, connections)

### Quarterly Tasks
- [ ] Security audit of authorization logic
- [ ] Disaster recovery drill (restore from backup)
- [ ] Review and update monitoring thresholds
- [ ] Architecture review for scale improvements

---

## Support Contacts

- **Deployment Issues:** DevOps team
- **Database Issues:** Database administrator
- **Security Concerns:** Security team
- **Customer Questions:** Support/Product team
