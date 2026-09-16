# Integration Events

This document defines the marketplace integration-event contracts used across seller onboarding, catalog listing, ordering, and payout workflows.

## Standard Event Envelope

All events include these envelope fields in addition to the event-specific payload:

| Field | Type | Description |
| --- | --- | --- |
| `eventId` | string | Unique event identifier. |
| `eventType` | string | Contract name, such as `SellerRegistered`. |
| `eventVersion` | string | Explicit contract version, such as `v1` or `v2`. |
| `occurredAtUtc` | string (`date-time`) | UTC timestamp for when the event was produced. |

## Versioning Strategy

- Add new fields only.
- Never remove or repurpose published fields in-place.
- Breaking structural changes require a new version.
- `v1` consumers must ignore unknown fields added in `v2`.
- During version migration, producers may publish `v1` and `v2` side by side until all required consumers complete cutover.

## Event Contracts

### 1. SellerRegistered

- **Name:** `SellerRegistered`
- **Version:** `v1`
- **Published by:** `Sellers.API`
- **Consumed by:** `Catalog.API`, `Webhooks.API`, `Analytics`, `Notifications`

#### Schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `sellerId` | string | Yes | Unique seller identifier. |
| `email` | string | Yes | Seller contact email. |
| `firstName` | string | Yes | Seller first name. |
| `lastName` | string | Yes | Seller last name. |
| `commissionRate` | number | Yes | Decimal commission rate between `0` and `1`. |

#### Example Payload

```json
{
  "eventId": "f2f76d6a-9703-49e3-a230-17620888d741",
  "eventType": "SellerRegistered",
  "eventVersion": "v1",
  "occurredAtUtc": "2026-09-15T21:00:00Z",
  "sellerId": "5d6af6d6-0d7c-4df8-9752-894d83dfab4e",
  "email": "seller@example.com",
  "firstName": "Ava",
  "lastName": "Brooks",
  "commissionRate": 0.15
}
```

### 2. ProductListedBySeller

- **Name:** `ProductListedBySeller`
- **Version:** `v1`
- **Published by:** `Catalog.API`
- **Consumed by:** `Search Indexing`, `Recommendations`, `Webhooks.API`

#### Schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `productId` | string | Yes | Unique product identifier. |
| `productName` | string | Yes | Display name for the listed product. |
| `sellerId` | string | Yes | Owning seller identifier. |
| `price` | number | Yes | Current listed unit price. |

#### Example Payload

```json
{
  "eventId": "ec4ec9d9-92e9-45c3-a722-5f78d9486fe8",
  "eventType": "ProductListedBySeller",
  "eventVersion": "v1",
  "occurredAtUtc": "2026-09-15T21:05:00Z",
  "productId": "1001",
  "productName": "Noise Cancelling Headphones",
  "sellerId": "5d6af6d6-0d7c-4df8-9752-894d83dfab4e",
  "price": 199.99
}
```

### 3. OrderCreatedIntegrationEvent

- **Name:** `OrderCreatedIntegrationEvent`
- **Version:** `v2`
- **Published by:** `Ordering.API`
- **Consumed by:** `Sellers.API`, `Payments`, `Fulfillment`, `Webhooks.API`

#### Schema

`v2` adds an `orderLineItems` array so downstream consumers can calculate seller-specific payouts from immutable order-time values. Existing `v1` consumers must ignore the new field if they are still subscribed during migration.

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `orderId` | string | Yes | Unique order identifier. |
| `buyerId` | string | Yes | Buyer identifier. |
| `orderTotal` | number | Yes | Total order amount. |
| `currency` | string | Yes | ISO currency code. |
| `orderLineItems` | array | Yes | Per-line seller attribution and commission snapshot data. |

`orderLineItems[]`

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `productId` | string | Yes | Product identifier for the line item. |
| `productName` | string | Yes | Product display name captured at order time. |
| `quantity` | integer | Yes | Quantity ordered. |
| `unitPrice` | number | Yes | Unit price at order time. |
| `seller_id` | string \| null | No | Seller identifier for seller-owned items; `null` for platform-owned items. |
| `commission_rate` | number \| null | No | Immutable commission rate snapshot for the line item. |
| `commission_amount` | number \| null | No | Calculated commission amount for the line item. |
| `sellerAmount` | number \| null | No | Net seller amount for the line item after commission. |

#### Example Payload

```json
{
  "eventId": "8d8f2c1d-833b-4f25-87a5-182441e33e97",
  "eventType": "OrderCreatedIntegrationEvent",
  "eventVersion": "v2",
  "occurredAtUtc": "2026-09-15T21:10:00Z",
  "orderId": "ord-1001",
  "buyerId": "buyer-77",
  "orderTotal": 249.99,
  "currency": "USD",
  "orderLineItems": [
    {
      "productId": "1001",
      "productName": "Noise Cancelling Headphones",
      "quantity": 1,
      "unitPrice": 199.99,
      "seller_id": "5d6af6d6-0d7c-4df8-9752-894d83dfab4e",
      "commission_rate": 0.15,
      "commission_amount": 30.00,
      "sellerAmount": 169.99
    },
    {
      "productId": "2001",
      "productName": "Platform Gift Card",
      "quantity": 1,
      "unitPrice": 50.00,
      "seller_id": null,
      "commission_rate": null,
      "commission_amount": null,
      "sellerAmount": null
    }
  ]
}
```

### 4. SellerPayoutCreated

- **Name:** `SellerPayoutCreated`
- **Version:** `v1`
- **Published by:** `Sellers.API`
- **Consumed by:** `Finance`, `Reporting`, `Seller Portal`

#### Schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `payoutId` | string | Yes | Unique payout ledger identifier. |
| `sellerId` | string | Yes | Seller receiving the payout. |
| `orderId` | string | Yes | Source order identifier. |
| `grossAmount` | number | Yes | Gross amount attributable to the seller before commission. |
| `commissionAmount` | number | Yes | Commission withheld for the order payout. |
| `sellerAmount` | number | Yes | Net seller amount after commission. |

#### Example Payload

```json
{
  "eventId": "ea11b2ae-50d4-4a28-b47f-e9ce0ec4eab4",
  "eventType": "SellerPayoutCreated",
  "eventVersion": "v1",
  "occurredAtUtc": "2026-09-15T21:15:00Z",
  "payoutId": "087850c0-9e97-4493-b302-c5460f1c859f",
  "sellerId": "5d6af6d6-0d7c-4df8-9752-894d83dfab4e",
  "orderId": "ord-1001",
  "grossAmount": 199.99,
  "commissionAmount": 30.00,
  "sellerAmount": 169.99
}
```

### 5. SellerStatusChanged

- **Name:** `SellerStatusChanged`
- **Version:** `v1`
- **Published by:** `Sellers.API`
- **Consumed by:** `Catalog.API`, `Risk/Compliance`, `Webhooks.API`

#### Schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `sellerId` | string | Yes | Seller whose status changed. |
| `newStatus` | string | Yes | New seller lifecycle status. |
| `timestamp` | string (`date-time`) | Yes | Timestamp of the status transition. |

#### Example Payload

```json
{
  "eventId": "5516c35c-bf36-4c6e-9f31-8b9fa41788b8",
  "eventType": "SellerStatusChanged",
  "eventVersion": "v1",
  "occurredAtUtc": "2026-09-15T21:20:00Z",
  "sellerId": "5d6af6d6-0d7c-4df8-9752-894d83dfab4e",
  "newStatus": "suspended",
  "timestamp": "2026-09-15T21:20:00Z"
}
```
