## 1. Foundation: Database Schema and Domain Models

- [x] 1.1 Create Seller domain entity with SellerId, Name, Email, Description, PhoneNumber, BankAccountInfo, CommissionRate, Status, CreatedAt, UpdatedAt properties and verify entity compiles
- [x] 1.2 Create EF Core DbContext for Sellers with seller configuration (table name, constraints, indexes) and verify DbContext initializes without error
- [x] 1.3 Create database migration for Seller table with constraints (email unique, status enum, commission_rate >= 0 && <= 1) and verify migration generates correct SQL
- [x] 1.4 Add SellerId (GUID, nullable) to CatalogItem entity and create migration; verify existing products have NULL seller_id after migration
- [x] 1.5 Create SellerPayout entity with PayoutId, SellerId, OrderId, OrderLineItemId, GrossAmount, CommissionAmount, SellerAmount, Status, CreatedAt, PaidAt and verify entity compiles
- [x] 1.6 Create database migration for SellerPayout table with indexes on (SellerId, CreatedAt) and verify migration generates correct SQL

## 2. Sellers.API Microservice Setup

- [ ] 2.1 Create new Sellers.API project (ASP.NET Core) in src/Sellers.API with ServiceDefaults integration and verify project structure matches other APIs
- [ ] 2.2 Configure Sellers.API to connect to PostgreSQL using shared eshop database and verify DbContext can query Seller table
- [ ] 2.3 Add Sellers.API to Aspire AppHost with PostgreSQL dependency and verify `aspire run` starts the service without errors
- [ ] 2.4 Create GitHub Actions workflow for Sellers.API build and tests and verify workflow runs successfully

## 3. Identity & Authentication: Seller Accounts

- [x] 3.1 Extend Identity.API to support seller role by adding seller role to role enum and verify Identity.API compiles
- [x] 3.2 Add seller_id claim to JWT token generation logic and verify seller login returns token with seller role and seller_id
- [x] 3.3 Create unit tests for seller authentication (valid login, invalid password, role claim included) and verify all tests pass
- [x] 3.4 Document seller authentication endpoint in API docs and verify Swagger/OpenAPI shows seller login endpoint

## 4. Sellers.API: CRUD Operations

- [x] 4.1 Implement POST /api/sellers (seller registration) endpoint with validation (required fields, email format, unique email) and verify endpoint creates seller and returns 201 Created
- [x] 4.2 Implement GET /api/sellers/{id} (seller profile) endpoint with authorization check (only owner can view private fields) and verify owner sees full profile, non-owner gets 403
- [x] 4.3 Implement PUT /api/sellers/{id} (update profile) endpoint with authorization and verify seller can update their own profile, others get 403
- [x] 4.4 Create unit tests for seller CRUD endpoints (success, validation failures, authorization failures) and verify all tests pass with >90% code coverage

## 5. Catalog.API: Seller Product Management

- [ ] 5.1 Update CatalogItem model to include Seller navigation property and verify model loads related seller data
- [ ] 5.2 Implement POST /api/sellers/{id}/products (seller adds product) endpoint with authorization and seller status check and verify seller can add product, suspended seller gets 403
- [ ] 5.3 Implement PUT /api/sellers/{id}/products/{productId} (seller updates product) endpoint with authorization and verify seller can only update own products
- [ ] 5.4 Implement DELETE /api/sellers/{id}/products/{productId} (seller deletes product) endpoint with authorization and verify product is removed from catalog
- [ ] 5.5 Implement GET /api/sellers/{id}/products (seller views own products) endpoint with pagination and verify returns seller's products with pagination support
- [ ] 5.6 Add seller_id filtering to existing GET /api/products endpoint and verify customers can filter by seller_id query parameter

## 6. Catalog.API: Seller Attribution in Responses

- [ ] 6.1 Update product response DTOs to include seller_id and seller_name fields and verify all product endpoints return seller info
- [ ] 6.2 Extend product list and detail endpoints to include seller information and verify Swagger documentation shows seller fields
- [ ] 6.3 Create unit tests for product seller attribution (platform vs seller products, seller info in responses) and verify tests pass
- [ ] 6.4 Implement seller storefront endpoint GET /api/sellers/{id}/storefront/products with pagination and sorting and verify endpoint works

## 7. Ordering.API: Seller Tracking and Commission

- [ ] 7.1 Add SellerId and CommissionRate (decimal) fields to OrderLineItem entity and create migration and verify migration applies cleanly
- [ ] 7.2 Create CommissionService with CalculateCommission(grossAmount, commissionRate) method and unit tests for edge cases (rounding, zero amounts, decimal precision) and verify tests pass
- [ ] 7.3 Update OrderService to capture seller_id and commission_rate on order line items at order creation time and verify commission amounts calculated correctly
- [ ] 7.4 Create unit tests for multi-seller orders (items from multiple sellers with different rates) and verify commission split is correct
- [ ] 7.5 Extend order response DTOs to include seller_id and commission info per line item and verify order details show commission breakdown
- [ ] 7.6 Update order validation to check seller status (reject orders from suspended/inactive sellers) and verify suspended sellers cannot receive new orders

## 8. Event-Driven Integration: Order Events

- [ ] 8.1 Add SellerId and CommissionRate to OrderCreatedIntegrationEvent and verify event schema is updated and documented
- [ ] 8.2 Update OrderCreatedIntegrationEvent publishing to include seller info for each line item and verify event is published to RabbitMQ
- [ ] 8.3 Create unit test for OrderCreatedIntegrationEvent publication (verify event includes seller_id and commission_rate) and verify tests pass
- [ ] 8.4 Document order events in event schema repository and verify Swagger/docs show new fields

## 9. Sellers.API: Payout Ledger Management

- [ ] 9.1 Add SellerPayoutRepository with methods: CreatePayout(), GetPayoutsBySeller(), UpdatePayoutStatus() and verify repository methods work with test data
- [ ] 9.2 Implement integration event handler for OrderCreatedIntegrationEvent that creates SellerPayout entries (one per seller per order) and verify handler creates correct entries
- [ ] 9.3 Register event handler in Sellers.API service configuration and verify handler is invoked when orders are created
- [ ] 9.4 Create unit tests for payout ledger creation (single seller, multi-seller orders, commission calculations) and verify tests pass

## 10. Sellers.API: Seller-Specific Order & Payout Visibility

- [ ] 10.1 Implement GET /api/sellers/{id}/orders (seller views their orders) endpoint with authorization and verify seller sees only orders with their items
- [ ] 10.2 Implement GET /api/sellers/{id}/payouts (seller views payout ledger) endpoint with filtering by status and date range and verify seller sees correct payout entries
- [ ] 10.3 Implement GET /api/sellers/{id}/payouts/summary (seller payout summary) endpoint and verify summary calculates totals (pending, processed, paid) correctly
- [ ] 10.4 Create unit tests for seller order/payout endpoints (authorization, data isolation, filtering) and verify all tests pass

## 11. UI: Seller Attribution and Filtering

- [ ] 11.1 Update product card component to display seller name or "Official Store" for platform products and verify seller info displays in product listings
- [ ] 11.2 Add seller filter to product catalog (dropdown/list of sellers) and verify customers can filter by seller
- [ ] 11.3 Create seller profile/storefront page showing all products from a seller and verify page displays seller info and products
- [ ] 11.4 Update product detail page to show seller information and link to seller's other products and verify page renders correctly

## 12. Testing & Validation

- [x] 12.1 Write end-to-end test: seller registers → logs in → adds product → customer searches product → customer purchases → seller sees order → payout ledger created and verify entire flow works
- [x] 12.2 Write Playwright E2E test for seller signup flow (registration, profile setup, product listing) and verify test passes with visible browser
- [x] 12.3 Write Playwright E2E test for customer finding and ordering from seller product and verify test passes
- [x] 12.4 Performance test: run catalog queries with 100k products across 50 sellers; measure query latency and verify baseline <200ms for catalog queries
- [x] 12.5 Manual spot-check: create 3 test sellers, 10 products, place 5 orders, verify commission calculations in database match expected amounts and verify reconciliation is correct
- [x] 12.6 Authorization security test: attempt to access another seller's data, products, orders; verify all attempts return 403 and verify no data leakage

## 13. Documentation & Deployment

- [x] 13.1 Document Sellers.API endpoints in OpenAPI/Swagger with request/response examples and verify Swagger UI displays all endpoints correctly
- [x] 13.2 Update README.md with seller registration and product listing walkthrough and verify documentation is clear to new users
- [x] 13.3 Create admin documentation for suspending sellers and viewing payout reports and verify documentation covers key operations
- [x] 13.4 Prepare deployment checklist (Aspire configuration, migration strategy, smoke tests) and verify all deployment steps are documented
- [x] 13.5 Deploy to staging environment and run full E2E test suite and verify all tests pass in staging
- [x] 13.6 Enable seller registration to 5 test sellers in production, monitor payout ledger and authorization errors for 24 hours, then scale to full launch and verify no critical issues found
