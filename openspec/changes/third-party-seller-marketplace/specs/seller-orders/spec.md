## Purpose

Allows sellers to view and monitor orders containing their products, track order status, and manage fulfillment for seller-owned items.

## ADDED Requirements

### Requirement: Seller can view their orders
The system SHALL provide an endpoint for sellers to view all orders containing their products. Sellers SHALL see order details including order ID, date, customer information (limited for privacy), order items they sold, pricing, and order status.

#### Scenario: Seller views all their orders
- **WHEN** a seller requests GET /api/sellers/{id}/orders with valid authentication
- **THEN** the system returns a list of all orders containing that seller's products

#### Scenario: Order list includes only seller's items
- **WHEN** a seller views orders for their items, and an order contains items from multiple sellers
- **THEN** the system returns the complete order but highlights/filters to show only items from that seller

#### Scenario: Seller cannot view other sellers' orders
- **WHEN** a seller attempts to view GET /api/sellers/{otherId}/orders without authorization
- **THEN** the system returns 403 Forbidden

#### Scenario: Order list pagination
- **WHEN** a seller has more than 20 orders and requests GET /api/sellers/{id}/orders?page=2&pageSize=20
- **THEN** the system returns the second page of results with 20 orders

### Requirement: Order details include seller context
Each order SHALL show seller-specific context including the portion of revenue earned by the seller (gross amount minus platform commission). Sellers SHALL see item-level pricing and their calculated earnings per item.

#### Scenario: Seller views order with commission breakdown
- **WHEN** a seller requests GET /api/sellers/{id}/orders/{orderId}
- **THEN** the system returns order details with seller_gross_amount, commission_amount, and seller_amount fields for each line item

#### Scenario: Multi-seller order shows seller's portion
- **WHEN** an order contains items from multiple sellers, a seller views their portion
- **THEN** each order shows only their items with their pricing and calculated earnings

#### Scenario: Order includes price and commission rate
- **WHEN** a seller views an order line item
- **THEN** the system displays unit_price, quantity, gross_total, commission_rate (e.g., 0.15), commission_amount, and seller_amount

### Requirement: Order status tracking
The system SHALL track and display order status for each line item from each seller. Status values include Pending, Processing, Shipped, Delivered, and Cancelled. Sellers SHALL be able to see the current status of orders containing their products.

#### Scenario: Seller sees order status
- **WHEN** a seller views an order
- **THEN** the order displays current status (Pending, Processing, Shipped, Delivered, or Cancelled)

#### Scenario: Status updates reflect across sellers
- **WHEN** an order status changes (e.g., from Processing to Shipped)
- **THEN** the status update is visible to all sellers with items in that order

#### Scenario: Cancelled orders show cancellation reason
- **WHEN** an order is cancelled
- **THEN** the order shows status "Cancelled" with an optional cancellation reason

### Requirement: Order filtering and search
The system SHALL support filtering and searching seller orders by date range, status, and order ID. Sellers SHALL be able to find specific orders efficiently.

#### Scenario: Seller filters orders by status
- **WHEN** a seller requests GET /api/sellers/{id}/orders?status=Shipped
- **THEN** the system returns only orders with that status

#### Scenario: Seller filters orders by date range
- **WHEN** a seller requests GET /api/sellers/{id}/orders?fromDate=2026-01-01&toDate=2026-01-31
- **THEN** the system returns orders created within that date range

#### Scenario: Seller searches orders by order ID
- **WHEN** a seller requests GET /api/sellers/{id}/orders?search=ORDER-12345
- **THEN** the system returns matching order

### Requirement: Seller order count and totals
The system SHALL provide summary information for sellers including total order count, total revenue, and pending payout amounts. This helps sellers track business performance.

#### Scenario: Seller views order summary
- **WHEN** a seller requests GET /api/sellers/{id}/orders/summary
- **THEN** the system returns total_orders, total_revenue, total_earned_by_seller, and pending_payout_amount

#### Scenario: Summary reflects current state
- **WHEN** a seller views order summary and a new order is placed
- **THEN** summary updates immediately to reflect the new order
