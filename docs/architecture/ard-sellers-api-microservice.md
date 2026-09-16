# Architecture Decision: Separate Sellers.API Microservice

**Date:** 2026-09-15  
**Status:** Decided

## Context

The eShop platform is evolving from a single-vendor storefront into a marketplace that supports third-party sellers. This change introduces a new business capability: managing seller onboarding, seller profiles, operational status, payout visibility, and seller-specific policies such as compliance review and commission handling. Those responsibilities are materially different from the existing concerns of Catalog.API and Identity.API.

Catalog.API is responsible for product data, browse and search behavior, and customer-facing catalog experiences. Identity.API is responsible for authentication, authorization, and token issuance. Neither service is a natural home for seller lifecycle management. Embedding seller management into one of those existing services would make the design harder to reason about because it would combine infrastructure concerns with a new marketplace domain that has its own workflow, data model, and future roadmap.

We therefore needed a service boundary that could own seller business capabilities directly, expose seller-focused APIs, and participate in event-driven workflows without overloading unrelated services.

## Decision

We chose to create a separate **Sellers.API** microservice to own seller management capabilities for the marketplace.

## Rationale

**Domain separation.** Creating Sellers.API gives the platform a clear separation of concerns. Catalog and seller management are related, but they are not the same domain. The catalog manages products and product discovery; seller management governs who may operate as a seller, what status that seller holds, and how seller-specific business rules are enforced. Keeping these concerns separate prevents the catalog service from accumulating marketplace account logic that does not belong there.

**Independent scaling.** This decision supports independent resource allocation. Seller registration, profile updates, payout queries, and operational checks have different traffic and performance characteristics from product browsing and catalog search. A separate service lets the team scale seller workloads independently instead of scaling catalog infrastructure to absorb unrelated seller activity.

**Event boundaries.** Sellers.API creates cleaner event boundaries for marketplace orchestration. Seller events such as **SellerRegistered**, **SellerActivated**, or **SellerSuspended** can be published independently and consumed by other services without coupling those contracts to catalog internals or identity flows. This improves clarity in event-driven integration and makes the seller domain easier to evolve over time.

**Future extensibility.** A dedicated service is easier to extend later. Marketplace growth is likely to require seller dashboards, analytics, compliance checks, payout administration, audit trails, and operational tooling. Those features fit naturally in Sellers.API and can be added without turning Catalog.API or Identity.API into overloaded mixed-responsibility services. This decision also future-proofs the architecture against the addition of seller-specific features that would be difficult to retrofit into existing services.

## Alternatives Considered

### Embed seller management in Catalog.API

This option was rejected because it mixes product catalog responsibilities with seller account and operational business logic. It would also couple scaling for seller workflows to catalog traffic, creating avoidable operational inefficiency and a blurrier domain boundary.

### Embed seller management in Identity.API

This option was rejected because it confuses authentication and authorization with seller business account management. Identity should answer who a caller is and what they are allowed to do; it should not become the long-term owner of seller profiles, business status, payout views, or marketplace operations.

## Consequences

Positive consequences include clearer service boundaries, independent deployments, and the ability to evolve seller features without destabilizing catalog or identity responsibilities.

Negative consequences include added cross-service coordination and additional database connections or data-access paths where seller data must interact with catalog or ordering workflows.

The main risk is network latency between services. We mitigate that by keeping seller-owned data local to Sellers.API, using integration events for propagation, and avoiding synchronous cross-service calls unless they are required for correctness.

## Related Decisions

- **ADR 2:** Single PostgreSQL Database for MVP
- **ADR 3:** Nullable SellerId Design Pattern
- **ADR 4:** Commission Immutable Storage at Order Time
