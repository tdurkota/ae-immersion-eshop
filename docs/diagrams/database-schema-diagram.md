# Database Schema ER Diagram

This diagram captures the proposed marketplace schema updates for seller ownership, immutable order-time commission snapshots, and payout ledger tracking.

```mermaid
erDiagram
    %% NEW TABLE: Seller
    %% Indexes: UNIQUE(Email), INDEX(Email), INDEX(Status, CreatedAt)
    Seller {
        guid SellerId PK
        string Email "UNIQUE, NOT NULL"
        string FirstName "NOT NULL"
        string LastName "NOT NULL"
        text Description "NULL"
        string PhoneNumber "NULL"
        decimal CommissionRate "NOT NULL, 0..1"
        enum Status "NOT NULL: active|suspended|inactive"
        timestamp CreatedAt
        timestamp UpdatedAt
    }

    %% UPDATED TABLE: CatalogItem
    %% Indexes: INDEX(SellerId), INDEX(SellerId, CreatedAt)
    CatalogItem {
        guid ProductId PK
        string Name "existing, NOT NULL"
        text Description "existing"
        decimal Price "existing, NOT NULL"
        string ImageUrl "existing"
        int CatalogTypeId "existing, NOT NULL"
        int CatalogBrandId "existing, NOT NULL"
        int AvailableStock "existing, NOT NULL"
        int RestockThreshold "existing, NOT NULL"
        int MaxStockThreshold "existing, NOT NULL"
        boolean OnReorder "existing, NOT NULL"
        guid SellerId FK "NULL -> Seller.SellerId"
        timestamp CreatedAt
        timestamp UpdatedAt
    }

    %% EXISTING TABLE REFERENCE
    Order {
        int OrderId PK
        string CustomerId FK
        timestamp OrderDate
        string Status
        string Description
    }

    %% UPDATED TABLE: OrderLineItem
    %% Indexes: INDEX(OrderId), INDEX(SellerId), INDEX(OrderId, SellerId)
    OrderLineItem {
        int OrderLineItemId PK
        int OrderId FK "NOT NULL -> Order.OrderId"
        guid ProductId FK "NOT NULL -> CatalogItem.ProductId"
        int Quantity "existing, NOT NULL"
        decimal Price "existing, NOT NULL"
        guid SellerId FK "NULL -> Seller.SellerId"
        decimal CommissionRate "NULL, immutable snapshot"
        decimal CommissionAmount "NULL, immutable snapshot"
        decimal SellerAmount "NULL, immutable snapshot"
        timestamp CreatedAt
    }

    %% NEW TABLE: SellerPayout
    %% Indexes: INDEX(SellerId, CreatedAt), INDEX(Status, CreatedAt)
    SellerPayout {
        guid PayoutId PK
        guid SellerId FK "NOT NULL -> Seller.SellerId"
        int OrderId FK "NULL -> Order.OrderId"
        int OrderLineItemId FK "NULL -> OrderLineItem.OrderLineItemId"
        decimal GrossAmount "NOT NULL"
        decimal CommissionAmount "NOT NULL"
        decimal SellerAmount "NOT NULL"
        enum Status "NOT NULL: pending|processed|paid|failed"
        timestamp CreatedAt
        timestamp PaidAt "NULL"
    }

    Seller ||--o{ CatalogItem : "owns products"
    Seller ||--o{ OrderLineItem : "credited on sale"
    Seller ||--o{ SellerPayout : "receives payouts"
    Order ||--o{ OrderLineItem : "contains"
    CatalogItem ||--o{ OrderLineItem : "ordered as"
    Order o|--o{ SellerPayout : "optional source order"
    OrderLineItem o|--o{ SellerPayout : "optional payout source"
```

## Referential Integrity Notes

- `CatalogItem.SellerId` is nullable to preserve platform-owned catalog items; `NULL` means the product is owned by the platform.
- `OrderLineItem` stores `SellerId`, `CommissionRate`, `CommissionAmount`, and `SellerAmount` as immutable order-time snapshots for auditability and reconciliation.
- `SellerPayout` references `Seller` as required, while `Order` and `OrderLineItem` remain optional to support current per-line payouts and future batched payout workflows.

> Data lifecycle: Seller status changes to `suspended` → Orders referencing that seller still show commission (immutable); new orders from suspended sellers are rejected.
