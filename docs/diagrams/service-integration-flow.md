# Service Integration Flow

This diagram summarizes the end-to-end marketplace seller flow across identity, seller management, catalog publishing, ordering, and payout tracking.

```mermaid
flowchart LR
    Seller[Seller User]

    subgraph Services[Platform Services]
        Identity[Identity.API]
        Sellers[Sellers.API]
        Catalog[Catalog.API]
        Ordering[Ordering.API]
        Rabbit[(RabbitMQ)]
        Postgres[(PostgreSQL)]
    end

    Seller -->|1. Submit seller registration| Sellers
    Sellers -->|2. Create seller profile| Postgres
    Sellers -->|3. Request seller role assignment| Identity
    Identity -->|4. Add Seller role + seller_id claim| Postgres
    Identity -->|5. Return seller-enabled JWT| Seller

    Seller -->|6. Create product with seller JWT| Catalog
    Catalog -->|7. Validate seller identity/role| Identity
    Catalog -->|8. Store product linked to seller_id| Postgres
    Catalog -->|9. Publish ProductCreated / ProductUpdated| Rabbit

    Rabbit -->|10. Product availability event consumed| Ordering
    Seller -.->|Catalog data available for checkout| Ordering
    Ordering -->|11. Create order with seller_id + commission snapshot| Postgres
    Ordering -->|12. Publish OrderSubmitted / OrderPaid| Rabbit

    Rabbit -->|13. Order event consumed for settlement| Sellers
    Sellers -->|14. Append payout ledger entry for seller| Postgres

    Sellers <-->|Seller profile/status lookups| Identity
    Catalog <-->|Seller ownership validation| Sellers
    Ordering <-->|Seller/product ownership context| Catalog
```

## Flow Notes

- `Identity.API` issues JWTs containing the `Seller` role and a `seller_id` claim after registration is approved.
- `Catalog.API` persists seller-owned products and emits catalog events through RabbitMQ for downstream consumers.
- `Ordering.API` stores immutable order-time seller tracking, including seller ownership and commission context.
- `Sellers.API` consumes order events to maintain the payout ledger in PostgreSQL for reconciliation and disbursement workflows.
