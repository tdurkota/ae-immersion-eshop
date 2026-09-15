# Architecture Decision: Single PostgreSQL Database for MVP

**Date:** 2026-09-15  
**Status:** Decided

## Context

The eShop marketplace MVP introduces three collaborating services: Sellers.API, Catalog.API, and Ordering.API. These services need to share data about sellers, products, and order line attribution while the product is still in a fast-moving implementation phase. The team must support workflows such as seller onboarding, initial product creation, catalog listing, and commission-aware ordering without slowing delivery through premature infrastructure complexity.

The long-term architecture may require independent scaling, stricter service isolation, and separate data ownership boundaries. However, those concerns are expected to emerge after the MVP proves the marketplace model and traffic patterns. For the MVP, the primary problem is balancing service-oriented boundaries with a practical data strategy that keeps development, testing, and operations simple.

## Decision

We will use a single shared PostgreSQL database cluster for the MVP. Sellers.API, Catalog.API, and Ordering.API will connect to the same PostgreSQL deployment and store their data in the shared eShop database.

## Rationale

This decision optimizes for speed, simplicity, and correctness during MVP delivery.

- **Simplicity:** A shared database avoids distributed transaction coordination between services. We do not need sagas, compensating actions, or 2-phase commit to keep closely related writes consistent during early marketplace flows.
- **ACID transactions:** Some MVP workflows benefit directly from atomic writes. For example, seller creation and creation of an initial product listing can be committed together when required, ensuring the system never exposes a partially initialized seller state.
- **Operational overhead:** Running and managing one PostgreSQL cluster is materially simpler than provisioning, securing, backing up, and monitoring multiple database clusters or instances. This reduces infrastructure setup time and lowers operational burden for the MVP team.
- **Migration path:** The decision does not change external service APIs. If service-level data isolation becomes necessary later, the data can be split into separate databases behind the existing service contracts without breaking consumers.

## Alternatives Considered

### Separate database per service

This would provide stronger service isolation and clearer long-term ownership boundaries. It was rejected for the MVP because cross-service workflows would require 2-phase commit, saga orchestration, or eventual consistency patterns earlier than necessary. It would also increase operational overhead by requiring multiple clusters, duplicated backup policies, and more complex local and CI environments.

### Event Sourcing with eventual consistency

Event sourcing could support a strong audit trail and future replay scenarios, but it was rejected as premature optimization for the MVP. It introduces modeling, storage, projection, and debugging complexity that does not align with the short delivery timeline or the need for simple transactional business flows.

## Consequences

**Positive consequences**
- Strong ACID guarantees for tightly coupled marketplace workflows
- Simpler schema evolution and database migrations during MVP iteration
- Lower operational complexity for deployment, monitoring, and recovery

**Negative consequences**
- Tighter coupling at the database layer between services
- Harder independent scaling if one workload grows faster than others
- Backup and restore operations are more tightly coupled across service data

**Risk**

The shared database may become a bottleneck as traffic, data volume, or service autonomy requirements increase. We will mitigate this through monitoring, query tuning, schema discipline, and a documented post-MVP sharding and separation plan.

## Migration Path

Post-MVP, we can split the shared database by introducing service-owned schemas and then moving those schemas into separate PostgreSQL databases or clusters. Each service should continue exposing the same HTTP and messaging contracts, so consumers remain unchanged. Shared reads should be replaced with API calls, replicated read models, or integration events. Data extraction can be phased by domain boundary: isolate seller data first, then catalog ownership, then ordering records. During migration, we can dual-write or publish change events, validate parity, and cut over service-by-service without breaking public APIs.

## Related Decisions

- **ARD 1:** Separate Sellers.API Microservice
- **ARD 3:** Nullable SellerId Design Pattern
