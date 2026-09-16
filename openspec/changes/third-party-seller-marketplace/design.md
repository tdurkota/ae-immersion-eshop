## Context

The eShop reference application uses a services-based architecture with Aspire orchestration. Current state:
- **Catalog.API**: Manages product catalog (platform-owned only)
- **Ordering.API**: Processes orders from customer basket
- **Identity.API**: Handles user authentication and authorization
- **Event Bus**: RabbitMQ-based event-driven integration (per EVENT_DRIVEN_ARCHITECTURE_BRD.md)
- **Database**: PostgreSQL for persistent data
- **UI**: Blazor Web components + ASP.NET Core web app

We are extending this architecture to support third-party sellers alongside platform products. See proposal.md for business motivation.

## Goals / Non-Goals

**Goals:**
- Enable third-party sellers to register and list products
- Maintain backward compatibility with existing platform products and order flows
- Implement transparent commission tracking and payout ledger
- Ensure role-based access control isolates seller data
- Provide foundation for future enhancements (dashboards, analytics, KYC)
- MVP launch in 4-5 weeks with barebones feature set

**Non-Goals:**
- Seller dashboards, analytics, or performance metrics (Phase 2+)
- Return management or dispute resolution (Phase 2+)
- Automated payout transfers to seller bank accounts (Phase 2+)
- Tiered commission rates or category-specific pricing (Phase 2+)
- Seller reviews/ratings or reputation system (Phase 2+)
- International seller support or multi-currency (Future)
- KYC/compliance verification (Phase 2+)

## Decisions

### 1. New Sellers.API Microservice
**Decision**: Create a dedicated Sellers.API microservice (new) rather than embedding seller management in Catalog.API.

**Rationale**:
- Separation of concerns: Catalog remains focused on product data; Sellers handles account/business logic
- Independent scaling: Seller management (profile, payouts) scales separately from catalog
- Clear event boundaries: Seller events can be published independently

**Alternatives Considered**:
- Embed in Catalog.API: Simpler initially but mixes domain concerns; harder to scale independently
- Embed in Identity.API: Would blur identity (authentication) with business accounts (seller identity)

**Implementation**: New ASP.NET Core service with own PostgreSQL database (same cluster for MVP; can separate later)

### 2. Single Database for MVP
**Decision**: Use single PostgreSQL database for Sellers.API, Catalog.API, and Ordering.API (same instance).

**Rationale**:
- Simplicity: Eliminates distributed transaction complexity
- ACID transactions: Seller creation and initial product can transact together if needed
- Migration path: Can shard into separate databases post-MVP without breaking APIs

**Alternatives Considered**:
- Separate database per service: Better isolation but introduces 2-phase commit complexity and operational overhead for MVP
- Event Sourcing: Great for audit trail but premature complexity

**Implementation**: All services connect to same postgres://host:port/eshop database; foreign keys enable referential integrity

### 3. Nullable SellerId on Products
**Decision**: Add optional `seller_id (GUID, nullable)` to CatalogItem. NULL = platform product; UUID = seller product.

**Rationale**:
- Backward compatible: Existing queries work without modification (NULL is ignored in most filters)
- Simple schema: No separate table needed; single query returns both platform and seller products
- Clear semantics: NULL explicitly means "platform owns this"

**Alternatives Considered**:
- Separate table for seller products: Cleaner isolation but requires join on every query; harder to show mixed results
- Special "platform" seller account: Could work but adds extra row; still requires similar logic

**Implementation**: EF Core migration adds column; existing products auto-NULL; index on seller_id for filtering

### 4. Commission Stored at Order Time
**Decision**: Store `commission_rate`, `commission_amount`, and `seller_amount` on OrderLineItem at order creation time (immutable thereafter).

**Rationale**:
- Historical accuracy: Past orders unaffected if seller's rate changes
- Audit trail: Complete record of what was charged when
- Simplicity: No runtime calculation needed; just read from order

**Alternatives Considered**:
- Calculate on-the-fly from Seller.CommissionRate: Simpler schema but historical orders change if rate updates; breaks reconciliation
- Commission as separate event: Adds complexity; calculation must still happen somewhere

**Implementation**: OrderService calculates commission, stores on LineItem; CommissionService provides logic

### 5. Event-Driven Payout Tracking
**Decision**: Use RabbitMQ events (existing infrastructure) to notify Sellers.API when orders occur. Sellers.API creates SellerPayout ledger entries.

**Rationale**:
- Decoupled: Ordering.API doesn't need to know about payouts; just publishes OrderCreated event
- Scalable: Event handler can retry, backoff, and handle out-of-order processing
- Audit trail: Event log provides complete ledger history

**Alternatives Considered**:
- Synchronous RPC call from Ordering to Sellers: Tightly coupled; if Sellers.API is down, checkout fails
- Direct database write from Ordering: Violates separation; hard to track who created the entry

**Implementation**: Ordering.API publishes OrderCreated event; Sellers.API has IIntegrationEventHandler<OrderCreatedIntegrationEvent>

### 6. Role-Based Access Control (RBAC)
**Decision**: Extend Identity.API to support seller role. Use JWT role claim and seller_id to enforce authorization on seller endpoints.

**Rationale**:
- Clear separation: Customer, Seller, Admin are distinct roles
- Testable: Authorization logic can be unit tested on role + seller_id
- Standards-based: JWT roles are familiar to .NET developers

**Alternatives Considered**:
- Attribute-based access control (ABAC): More flexible but overkill for MVP; adds complexity to policy engine
- Custom claims only: Possible but less discoverable than standard role claims

**Implementation**: Add seller role to Identity.API; Sellers.API uses [Authorize(Roles = "seller")] and validates seller_id in handler

### 7. Instant Seller Signup (No Verification MVP)
**Decision**: Allow sellers to register and list products immediately without email verification or business verification.

**Rationale**:
- MVP speed: Enables fast onboarding for early testers
- De-risked: Can add verification logic post-launch without changing APIs
- Seller status field: Allows admin to suspend bad actors retroactively

**Alternatives Considered**:
- Email verification: Adds email queue dependency; slightly slower MVP
- KYC verification: Out of scope for MVP; adds significant complexity

**Implementation**: Seller.Status defaults to Active; can be changed by admin; Seller.EmailVerified added but not enforced in MVP

### 8. Hybrid Catalog (Platform + Sellers)
**Decision**: Both platform and seller products coexist in same catalog. UI shows seller attribution; filtering optional.

**Rationale**:
- Simple UX: One shopping experience for all products
- Revenue hedging: Platform can continue selling own products
- Clear ownership: Seller field makes accountability transparent

**Alternatives Considered**:
- Separate marketplace section: Confusing UX; splits catalog
- Sellers only: Loses platform revenue; requires migration of existing products

**Implementation**: Catalog queries return all products; UI optional filter; product detail shows seller info

### 9. Multi-Seller Checkout (Single Transaction)
**Decision**: Customers can checkout with items from multiple sellers in a single transaction. One order is created with multiple line items (one per seller) and paid in a single payment to the platform payment processor.

**Rationale**:
- Simplified UX: Customers expect a unified checkout experience (like Amazon/eBay)
- Platform payment processing: Single payment simplifies reconciliation and reduces payment processor fees
- Commission split at order time: Each line item stores seller commission separately, enabling proper revenue attribution
- Multi-seller order specification: Orders spec explicitly requires multi-seller orders to be supported

**Alternatives Considered**:
- Separate checkouts per seller: Each seller gets a checkout flow; worse UX; customers must check out multiple times
- Seller-specific payment processing: Each seller has their own payment processor; operational complexity; reconciliation nightmare

**Implementation**: OrderService accepts multi-seller baskets; creates single order with multiple line items; payment processor receives order total; commission split happens at line-item level (see Decision 4)

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **Authorization Bypass** - seller can access other seller data | CRITICAL | Strong unit tests for authorization handlers; integration tests; manual spot-checks; code review required |
| **Revenue Leakage** - commission calculation wrong, money lost | CRITICAL | Unit tests for all commission scenarios; reconciliation audit query; manual spot-checks on first orders; clear audit trail in database |
| **Backward Compatibility** - existing queries break with seller_id | HIGH | seller_id is nullable; existing code doesn't require changes; comprehensive test coverage for both NULL and UUID cases |
| **Performance Degradation** - queries slow on large product set | MEDIUM | Index on seller_id in database; query performance testing; monitor query plans; consider caching if needed |
| **Event Processing Failures** - payout entries not created if Sellers.API down | MEDIUM | Dead-letter queue for failed events; manual retry mechanism; detailed logging; Sellers.API treats event handler as critical |
| **Seller Abuse** - bad actors register and sell counterfeit goods | MEDIUM | Admin suspension capability; audit logging of seller actions; plan KYC for Phase 2; monitor reports |
| **Data Inconsistency** - seller/product state out of sync between services | MEDIUM | Event-driven eventual consistency acceptable; admin tools to rebuild state if needed |
| **Multi-Seller Order Edge Cases** - split fulfillment logic unclear | MEDIUM | Spec detail; clear scenarios; Ordering.API owns fulfillment coordination; Sellers.API read-only on orders |

## Migration Plan

**Phase Deployment**:
1. Deploy Sellers.API (new service) with EF migrations
2. Deploy Identity.API changes (seller role support)
3. Deploy Catalog.API changes (seller_id field, new endpoints)
4. Deploy Ordering.API changes (commission, event publishing)
5. Deploy UI updates (seller attribution, filtering)
6. Enable seller registration and test flow end-to-end

**Rollback Strategy**:
- Sellers.API: Disable seller registration; existing products remain visible
- Catalog.API: seller_id remains but ignored; no API changes are breaking
- Ordering.API: Commission tracking is additive; old orders have NULL commission data (acceptable)
- Identity.API: Seller role doesn't affect existing customers
- No data loss required; all changes backward compatible

**Canary Deployment** (Recommended):
- Deploy to staging environment first
- Run E2E test suite: seller registration → product listing → customer order
- Enable seller registration to 5-10 test sellers in production
- Monitor payout ledger reconciliation, authorization errors, event processing
- Scale to full launch after 1-2 days of validation

## Open Questions

1. **Minimum Payout Threshold**: Should there be a minimum earned amount before sellers can request payout (e.g., $100)? Deferred to post-MVP if needed.
2. **Seller Suspension Retroactivity**: When a seller is suspended, should active orders continue fulfillment? Assume yes (orders complete, seller just blocked from new orders).
3. **Bank Account Collection**: In MVP, do we collect seller bank details during registration, or defer until Phase 2? Assuming deferred (placeholder only).
4. **Tax Handling**: Are we collecting seller tax IDs? Out of scope for MVP; assume platform handles tax per-jurisdiction.
