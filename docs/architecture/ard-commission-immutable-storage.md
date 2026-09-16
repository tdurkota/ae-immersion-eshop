# Architecture Decision: Commission Immutable Storage at Order Time

**Date:** 2026-09-15  
**Status:** Decided

## Context

The platform must calculate and track seller commissions for each purchased order line. A seller's commission rate may change over time because of plan changes, negotiated contracts, promotional periods, or operational corrections. At the same time, finance, support, and seller operations need a historically accurate record of what commission was charged and what payout was owed when the buyer completed checkout.

If commission values are derived later from the current seller record, the platform risks changing the meaning of already-placed orders. A rate update made days or months after purchase could alter reported revenue share, seller payout totals, and dispute analysis for historical orders. That would create instability in accounting and make it difficult to explain why an order was charged a certain commission at the time it was created.

The core question is therefore: how should commission data be calculated and persisted so that the order remains a durable business record?

## Decision

At order-creation time, the platform will calculate and store the following immutable commission fields on each `OrderLineItem`:

- `commission_rate`
- `commission_amount`
- `seller_amount`

These values are captured as a snapshot of the commercial terms in effect when the order line is created. After order creation, these fields must never be updated as part of seller profile changes, commission plan changes, or payout recalculation flows. Any downstream reporting, payout, refund, or audit logic must treat the stored values as the source of truth for that line item.

## Rationale

This decision preserves **historical accuracy**. Orders created under a prior seller rate remain correct even if that seller later moves to a different tier.

It creates a strong **audit trail**. Every order line contains the exact rate applied, the amount retained as commission, and the amount owed to the seller without requiring reconstruction from mutable reference data.

It improves **dispute resolution**. Support, finance, and sellers can review a clear record of the agreed economics at purchase time instead of debating which seller configuration was active later.

It also avoids **distributed consensus** during reconciliation. Payout and reporting workflows do not need to re-query the seller table and coordinate across changing records to determine what should have been charged.

## Alternatives Considered

### Store only `seller_id` and calculate from the current seller table

Rejected because seller rate changes would rewrite history. Historical orders would no longer reliably represent the commercial terms that existed when they were purchased.

### Event sourcing

Rejected because it introduces premature complexity for the current scope. While event sourcing can preserve history, immutable storage directly on `OrderLineItem` provides the required traceability with a much simpler operational model.

### Calculate commission on demand from the seller rate

Rejected because it weakens the audit trail and makes discrepancies harder to debug. Investigating finance issues would require reconstructing historical state instead of reading a stable order record.

## Consequences

**Positive:** The system gains a clear audit trail, historical accuracy, and simple payout calculation based on immutable order data.

**Negative:** If a seller rate changes mid-transaction, the platform must capture the intended snapshot consistently. This is mitigated by atomic order-line creation that calculates and stores the commission values in the same transaction.

**Risk:** A bug in commission calculation could be persisted to every created order. This is mitigated by strong unit test coverage on `CommissionService` and validation of rounding and payout scenarios.

## Edge Cases

- **Zero commission:** support seller tiers with a `commission_rate` of zero and ensure `commission_amount` is stored as zero.
- **Decimal rounding:** define and consistently apply currency precision rules when calculating `commission_amount` and `seller_amount`.
- **Refunds:** refunds should reduce `seller_amount` proportionally using the stored order-line commission snapshot rather than recalculating from current seller settings.

## Related Decisions

- **ADR 1:** Separate Sellers.API Microservice
- **ADR 2:** Single PostgreSQL Database for MVP
