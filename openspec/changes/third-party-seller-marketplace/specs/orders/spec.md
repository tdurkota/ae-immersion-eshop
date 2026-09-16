## Purpose

Handles order creation and management with support for multi-seller orders, tracking seller attribution and commission information for each order item.

## ADDED Requirements

### Requirement: Multi-seller orders
The system SHALL support orders containing products from multiple sellers. Each line item in an order SHALL include seller_id to track which seller provides that item. Order fulfillment and payment processing SHALL handle multiple sellers correctly.

#### Scenario: Customer orders from single seller
- **WHEN** a customer purchases multiple items from the same seller
- **THEN** the order contains one or more line items, all with the same seller_id

#### Scenario: Customer orders from multiple sellers
- **WHEN** a customer purchases items from two different sellers in a single checkout
- **THEN** the order contains line items with different seller_ids

#### Scenario: Order confirmation shows all sellers
- **WHEN** a customer completes an order with multiple sellers
- **THEN** the order confirmation displays each seller's items and seller information

### Requirement: Order line item seller tracking
Each order line item SHALL include seller_id, seller_name, and seller_status at time of order. This information SHALL be stored immutably with the order for historical accuracy even if seller information changes later.

#### Scenario: Line item stores seller snapshot
- **WHEN** an order is created
- **THEN** each line item records seller_id, seller_name, and seller_status (snapshot at order time)

#### Scenario: Historical seller data preserved
- **WHEN** a seller's name changes after an order is placed
- **THEN** the order still shows the seller name from when the order was created

#### Scenario: Line item seller info in order detail
- **WHEN** a customer views order details
- **THEN** each line item shows which seller provided that item

### Requirement: Commission calculation at order time
The system SHALL calculate and store commission information for each order line item at the time of order creation. Commission data includes commission_rate, commission_amount, and seller_amount (gross - commission).

#### Scenario: Commission stored on line item creation
- **WHEN** an order line item is created
- **THEN** the system stores commission_rate, commission_amount, and seller_amount on that line item

#### Scenario: Commission rate immutable after order
- **WHEN** a seller's commission rate changes after an order is placed
- **THEN** the order retains the commission_rate from time of order creation

#### Scenario: Seller amount calculated correctly
- **WHEN** viewing an order line item with gross_amount = $100 and commission_rate = 0.15
- **THEN** commission_amount = $15 and seller_amount = $85

### Requirement: Order validation for seller status
The system SHALL verify seller account status before accepting new orders. Orders from products by inactive or suspended sellers SHALL be rejected at checkout.

#### Scenario: Order rejected for suspended seller
- **WHEN** a customer attempts to checkout with a product from a suspended seller
- **THEN** the system returns validation error "This seller is currently suspended"

#### Scenario: Order rejected for inactive seller
- **WHEN** a customer attempts to checkout with a product from an inactive seller
- **THEN** the system returns validation error "This seller is not currently accepting orders"

#### Scenario: Platform products bypass seller check
- **WHEN** a customer purchases a platform product
- **THEN** no seller status validation is performed (no seller to validate)

### Requirement: Payment processing with seller commission
The system SHALL process customer payment for the full order amount. The platform SHALL capture the commission amount, and the remainder becomes available for seller payout.

#### Scenario: Payment processing captures commission
- **WHEN** customer payment is processed for a multi-seller order
- **THEN** the payment processor receives the full order total; system routes commission and seller amounts to respective accounts

#### Scenario: Multi-seller order payment
- **WHEN** an order contains $100 from Seller A (15% commission) and $50 from Seller B (10% commission)
- **THEN** customer pays $150 total; platform captures $22.50 commission; Seller A owes $85; Seller B owes $45

#### Scenario: Payment reconciliation
- **WHEN** calculating totals for an order
- **THEN** sum(seller_amounts) + sum(commission_amounts) = customer_payment_amount

### Requirement: Seller email notification on new orders
The system MAY send email notifications to sellers when they receive new orders. This feature is optional in MVP but SHALL support basic notification if implemented.

#### Scenario: Seller receives order notification
- **WHEN** an order containing a seller's products is completed
- **THEN** seller receives email with order details and link to view order in their dashboard

#### Scenario: Bulk orders single notification
- **WHEN** an order contains multiple items from the same seller
- **THEN** seller receives a single order notification (not per item)
