# Order Event Schema Documentation

## Overview
The Ordering.API publishes integration events to communicate order-related events to other microservices. These events are transmitted via RabbitMQ and contain seller attribution and commission information for multi-seller orders.

## OrderCreatedIntegrationEvent

Published when an order is successfully created in the system. This event triggers payout ledger creation in the Sellers.API microservice.

### Event Properties

| Property | Type | Description |
|----------|------|-------------|
| `OrderId` | `int` | The unique identifier of the created order |
| `BuyerName` | `string` | The name of the buyer who placed the order |
| `BuyerIdentityGuid` | `string` | The identity GUID/user ID of the buyer |
| `OrderLineItems` | `IEnumerable<OrderCreatedLineItem>` | Collection of line items in the order, each with seller and commission data |
| `CreationTime` | `DateTime` | Timestamp when the event was created (inherited from IntegrationEvent) |
| `Id` | `Guid` | Unique event ID for tracking (inherited from IntegrationEvent) |

### OrderCreatedLineItem Structure

Each line item in an order contains the following information:

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `OrderLineItemId` | `int` | Unique identifier for this line item | 0, 1, 2 |
| `SellerId` | `int` | ID of the seller providing this item | 1, 2, 3 |
| `ProductId` | `int` | ID of the product | 101 |
| `ProductName` | `string` | Name of the product | "Premium Widget" |
| `UnitPrice` | `decimal` | Price per unit | 100.00 |
| `Units` | `int` | Quantity ordered | 2 |
| `Discount` | `decimal` | Discount amount applied | 10.00 |
| `GrossAmount` | `decimal` | Total before commission: (UnitPrice × Units) - Discount | 190.00 |
| `CommissionRate` | `decimal` | Commission rate as decimal (0.0 to 1.0) | 0.15 |
| `CommissionAmount` | `decimal` | Platform commission: GrossAmount × CommissionRate | 28.50 |
| `SellerAmount` | `decimal` | Seller payout: GrossAmount - CommissionAmount | 161.50 |

### Example Event Payload (JSON)

```json
{
  "orderId": 12345,
  "buyerName": "John Doe",
  "buyerIdentityGuid": "550e8400-e29b-41d4-a716-446655440000",
  "orderLineItems": [
    {
      "orderLineItemId": 0,
      "sellerId": 1,
      "productId": 101,
      "productName": "Premium Widget",
      "unitPrice": 100.00,
      "units": 2,
      "discount": 10.00,
      "grossAmount": 190.00,
      "commissionRate": 0.15,
      "commissionAmount": 28.50,
      "sellerAmount": 161.50
    },
    {
      "orderLineItemId": 1,
      "sellerId": 2,
      "productId": 202,
      "productName": "Standard Gadget",
      "unitPrice": 50.00,
      "units": 1,
      "discount": 0.00,
      "grossAmount": 50.00,
      "commissionRate": 0.10,
      "commissionAmount": 5.00,
      "sellerAmount": 45.00
    }
  ],
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "creationTime": "2026-09-16T19:28:25.365Z"
}
```

## Event Publishing Flow

1. **Order Creation**: Customer places an order via POST /api/orders
2. **Order Validation**: Order items are validated, seller status checked
3. **Event Publication**: Two events are published in sequence:
   - `OrderStartedIntegrationEvent` - clears the shopping basket
   - `OrderCreatedIntegrationEvent` - triggers payout ledger creation
4. **RabbitMQ Transmission**: Events are sent to RabbitMQ for asynchronous processing
5. **Subscriber Processing**: 
   - Sellers.API listens for `OrderCreatedIntegrationEvent`
   - Creates SellerPayout entries for each seller's line items
   - One SellerPayout entry per line item (not consolidated)

## Commission Calculation

Commission amounts are calculated at order creation time using the CommissionService:

```
CommissionAmount = round(GrossAmount × CommissionRate, 2)
SellerAmount = round(GrossAmount - CommissionAmount, 2)
```

- Rounding strategy: Away From Zero (banker's rounding)
- Precision: 2 decimal places
- Example: $190 gross with 15% rate = $28.50 commission, $161.50 for seller

## Order Reconciliation

For multi-seller orders, the following reconciliation always holds true:

```
Sum(SellerAmount) + Sum(CommissionAmount) = Sum(GrossAmount)
```

## Integration with Payout System

When `OrderCreatedIntegrationEvent` is received by Sellers.API:

1. Payout ledger entries are created for each line item
2. Each SellerPayout is linked to:
   - OrderId: the order containing the item
   - OrderLineItemId: the specific line item
   - SellerId: the seller receiving payment
3. Status starts as "Pending" until payment is processed
4. Historical data is preserved even if seller information changes

## Related Events

- **OrderStartedIntegrationEvent**: Published when order is initiated (before payment)
- **OrderStatusChangedToPaidIntegrationEvent**: Published when order payment is confirmed
- **OrderStockConfirmedIntegrationEvent**: Published when inventory is confirmed

## Schema Versioning

Current schema version: 1.0

- Added in Section 8: Event-Driven Order Integration
- Supports multiple sellers per order
- Includes immutable commission data
- Enables payout ledger tracking

## API Documentation

The OrderCreatedIntegrationEvent schema is automatically included in the OpenAPI/Swagger documentation:
- Endpoint: `/swagger/index.html`
- Search for: "OrderCreatedIntegrationEvent"
- Contains: Full schema, properties, and example values
