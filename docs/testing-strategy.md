# Seller Marketplace Testing Strategy

## Purpose

This strategy defines how seller marketplace features should be tested across the platform so seller onboarding, catalog ownership, order attribution, commission calculation, and payout generation remain correct, secure, and observable as the system evolves.

## Scope

This document covers testing for:

- Seller registration and profile management
- Seller authorization and tenant isolation
- Product ownership and seller-linked catalog behavior
- Order validation for seller-owned items
- Commission calculation and payout generation
- Event-driven integrations between marketplace services

## 1. Test Pyramid

Adopt a pyramid-heavy test portfolio so most feedback comes from fast, isolated tests while a smaller set validates cross-service behavior.

### Unit Tests (70%)

Focus:

- Pure business logic
- Validation rules
- Mapping logic
- Authorization decision helpers
- Edge-case handling

Characteristics:

- Fully isolated
- Mock or fake external dependencies
- Deterministic and fast
- Run on every PR

Primary examples:

- Commission calculation
- DTO/entity validation
- Seller status rules
- Event-to-domain mapping

### Integration Tests (20%)

Focus:

- Service-to-service workflows
- Persistence behavior using a real database
- Messaging/event handlers
- Cache invalidation and read-model synchronization

Characteristics:

- Run against real infrastructure where feasible
- Validate Sellers.API → Ordering.API and related service interactions
- Exercise repositories, EF mappings, serializers, handlers, and API wiring together

Primary examples:

- Sellers.API → Ordering.API interactions
- Catalog persistence with `seller_id`
- Payout ledger creation after order placement

### Contract Tests (5%)

Focus:

- Consumer/provider compatibility between front-end and back-end
- DTO shape, field naming, nullability, and status-code guarantees

Characteristics:

- Validate request/response contracts independently of full E2E flows
- Protect against breaking API changes
- Should run whenever public API DTOs or controllers change

Primary examples:

- Front-end ↔ back-end contract verification for seller creation
- Order response schema verification including seller/commission fields

### E2E Tests (5%)

Focus:

- Critical user journeys spanning authentication, APIs, storage, and messaging

Characteristics:

- Minimal in count, high in value
- Cover only the most important platform workflows
- Execute in production-like environments when possible

Primary example:

- Seller registration → product listing → order creation → payout generation

## 2. Coverage Targets

Set explicit coverage thresholds and enforce them in CI.

- **All services:** >85% code coverage
  - Measured using **Cobertura** or **OpenCover**
- **Critical paths:** 100% coverage
  - Authorization logic
  - Commission calculation
- **Exception handling:** >80% coverage
  - Validation failures
  - Authorization failures
  - Event-processing failures
  - API error translation

Coverage should be evaluated per service and for aggregate marketplace-related components. Threshold failures should block merges when coverage drops below target.

## 3. Unit Tests to Write

### CommissionService

Test `Calculate(grossAmount, rate)` for:

- Standard percentage calculation
- `grossAmount = 0`
- `rate = 0`
- Decimal rounding to 2 places
- Very small fractional values
- Negative amount rejection
- Invalid rate rejection if handled in service

Specific scenarios:

- `Calculate(100.00, 0.10)` → `10.00`
- `Calculate(0, 0.10)` → `0.00`
- `Calculate(19.99, 0.05)` → correctly rounded 2-decimal result
- `Calculate(-10, 0.10)` → validation/exception

### SellerValidator

Test:

- Valid email accepted
- Invalid email rejected
- Commission rate lower bound accepted at `0`
- Commission rate upper bound accepted at `1`
- Commission rate below `0` rejected
- Commission rate above `1` rejected
- Allowed status enum values accepted
- Unknown/invalid status values rejected

### OrderValidator

Test:

- Reject orders from suspended sellers
- Accept orders from active sellers
- Validate `seller_id` presence on seller-owned line items
- Reject mismatched or missing `seller_id`
- Allow platform-only items without seller payout behavior where applicable

### SellerPayoutMapper

Test mapping from `OrderCreatedIntegrationEvent` to `SellerPayout`:

- Single-seller order maps correctly
- Multi-seller order creates separate payout records per seller
- Commission amount maps correctly per line item
- Zero-priced items produce zero commission and zero payout contribution
- Missing seller-owned items are ignored for payout creation

## 4. Integration Tests

### Seller registration → Identity.API adds seller role

Validate that:

- Creating a seller through Sellers.API results in identity role assignment
- Newly registered seller can access seller-only endpoints
- Missing role assignment causes expected authorization failure

### Seller creates product → Catalog.API stores with seller_id → Product appears in mixed catalog

Validate that:

- Product creation persists `seller_id`
- Seller-owned products coexist with platform-owned products
- Catalog queries return mixed ownership correctly
- Seller filters return only the seller’s products

### Multi-seller order creation → Commission calculated per line item → Payout ledger entries created

Validate that:

- Ordering accepts a cart with items from multiple sellers
- Commission is calculated at line-item granularity
- One payout ledger entry is created per seller
- Aggregated totals equal expected payout math

### Seller updates profile → Other services see changes

Validate that:

- Profile changes are visible to dependent services
- Cache invalidation occurs correctly, or
- Change events propagate and consumers refresh their read models

### Suspended seller → Orders rejected with 400 error

Validate that:

- Orders containing items from suspended sellers are rejected
- API returns `400`
- Error payload is stable and actionable
- Existing orders remain unaffected

## 5. Contract Tests

### Sellers.API `POST /sellers`

Verify response matches the seller creation DTO contract:

- Required fields present
- Optional fields nullable as expected
- Enum/string representations stable
- Field naming matches front-end expectations

### Ordering.API `GET /orders/{id}`

Verify response includes:

- Seller metadata needed by consumers
- Commission-related fields
- Expected numeric precision
- Stable nested line-item structure

### Authorization failure contract

Verify authorization denials return:

- `403 Forbidden`
- Not `401`
- Not `404`
- Consistent error body/shape across endpoints

## 6. Authorization Tests

These tests should exist at both unit/policy level and API/integration level where possible.

- Seller can view own profile → `200`
- Seller cannot view another seller’s profile → `403`
- Seller can update own products → `200`
- Seller cannot update another seller’s products → `403`
- Admin can view all sellers → `200`
- Missing JWT `seller_id` claim → `403`
- JWT `seller_id` claim mismatch → `403`

Additional checks:

- Seller role without matching ownership still denied
- Suspended seller remains blocked even with otherwise valid claims

## 7. Event Integration Tests

### OrderCreatedIntegrationEvent published → Sellers.API consumes → SellerPayout created

Validate:

- Event is published once for successful order creation
- Consumer processes event idempotently
- `SellerPayout` record is created with expected totals and seller ownership

### SellerPayoutCreated event published for reporting

Validate:

- Payout creation emits downstream event
- Reporting/read-model consumers receive and persist the event
- Duplicate delivery does not create duplicate reporting rows

### Multi-seller order → Multiple payout entries

Validate:

- One payout entry per seller
- Each payout references the correct order and seller
- Totals reconcile with the original order payload

## 8. Edge Cases & Negative Tests

Cover the following explicitly:

- Commission with `0.05` rate on a 3-item order rounds correctly to 2 decimals
- Seller with `0%` commission (free tier) yields `0` commission
- Free product (`price = 0`) with commission still produces `commission_amount = 0`
- Order with no seller products creates no payouts
- Seller becomes suspended mid-transaction
  - New orders rejected
  - Existing orders unaffected

Recommended additional negatives:

- Duplicate event delivery
- Missing `seller_id` on seller-owned product lines
- Deleted/inactive seller referenced by stale client data
- Decimal overflow or invalid currency precision in payloads

## 9. Performance Tests (Stretch Goal)

These are not required for every PR, but should be automated in scheduled or pre-release validation.

### Catalog query performance

Test:

- Listing **10,000 products** filtered by `sellerId`
- Validate index usage and acceptable latency

Success criteria:

- Query plan uses expected seller ownership index
- Response time remains within agreed SLA

### Payout summary performance

Test:

- Querying payout summary for a seller with **1,000 orders**
- Validate grouping, aggregation, and pagination performance

Success criteria:

- Query is optimized
- No N+1 access patterns
- Response latency remains within agreed SLA

## 10. Test Data / Fixtures

Create reusable seed data for marketplace scenarios:

- **3 sellers**
  - Active
  - Suspended
  - Inactive
- **10 products**
  - Mixed seller ownership
  - Include platform-owned items
  - Include at least one free product
- **5 orders**
  - Single-seller
  - Multi-seller
  - Platform-only
  - Suspended-seller rejection case

Use a factory-based test data approach:

- `SellerFactory`
- `ProductFactory`
- `OrderFactory`

Factory responsibilities:

- Provide sensible defaults
- Allow targeted overrides
- Generate deterministic IDs for stable assertions when needed
- Support scenario-specific compositions for multi-seller orders

## 11. CI/CD Integration

Integrate testing into delivery pipelines as follows:

- Run **all unit tests** on every PR
- Run **integration tests** on merge to `main`
  - These may be slower due to real DB and service dependencies
- Upload coverage report to GitHub via **Codecov**
- Fail pipeline if coverage drops below configured targets

Recommended pipeline gates:

- Unit tests required for PR completion
- Contract tests required when API contracts change
- Integration tests required before release or on protected branch merges
- E2E smoke suite required for deployment validation

## Recommended Execution Matrix

| Test Type | Scope | Frequency | Environment |
| --- | --- | --- | --- |
| Unit | Business logic, validators, mappers, auth helpers | Every PR | Test runner only |
| Integration | APIs, DB, events, cache behavior | Merge to `main` / nightly | Real DB + dependent services |
| Contract | DTOs, status codes, schema compatibility | Every PR affecting APIs | Consumer/provider test harness |
| E2E | Registration, listing, order, payout | Nightly / pre-release | Production-like stack |
| Performance | Large catalogs, payout summaries | Scheduled / pre-release | Performance environment |

## Definition of Done for Marketplace Testing

Marketplace functionality should be considered adequately tested when:

- Required unit, integration, contract, and E2E coverage exists
- Coverage thresholds are met
- Critical authorization and commission paths are fully covered
- Event-driven payout flows are validated for both single-seller and multi-seller orders
- Negative and edge-case scenarios pass reliably in CI
