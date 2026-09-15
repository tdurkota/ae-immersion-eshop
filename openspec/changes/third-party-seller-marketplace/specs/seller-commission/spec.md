## Purpose

Implements platform commission calculation and tracking on seller orders, ensuring transparent and auditable revenue split between platform and sellers.

## ADDED Requirements

### Requirement: Commission calculation
The system SHALL calculate commission on every order item sold by a seller. Commission is calculated as: commission_amount = seller_gross_amount × commission_rate. The seller receives: seller_amount = seller_gross_amount - commission_amount. Commission rate is set per seller at account creation and stored with each order for historical accuracy.

#### Scenario: Commission calculation for single-item order
- **WHEN** a customer purchases a product from a seller for $100 with a 15% commission rate
- **THEN** the system calculates commission_amount = $15, seller_amount = $85

#### Scenario: Commission calculation for multi-item order from same seller
- **WHEN** a customer purchases two products from same seller for $50 each (total $100) with 15% commission
- **THEN** the system calculates total_gross = $100, commission_amount = $15, seller_amount = $85

#### Scenario: Commission stored at order time
- **WHEN** an order is created with a seller's products
- **THEN** the system stores commission_rate and commission_amount on each line item for that order for audit trail

#### Scenario: Rate change doesn't affect past orders
- **WHEN** a seller's commission rate is updated after an order is placed
- **THEN** past orders retain their original commission_rate and commission_amount; only future orders use the new rate

### Requirement: Multi-seller order commission handling
The system SHALL correctly handle orders containing products from multiple sellers. Each line item SHALL have its own commission calculated based on that line item's seller and rate. The platform's total commission is the sum of all line item commissions.

#### Scenario: Order with items from two sellers
- **WHEN** a customer purchases Product A ($100, 15% commission, Seller 1) and Product B ($50, 15% commission, Seller 2)
- **THEN** platform_commission = $22.50, seller1_amount = $85, seller2_amount = $42.50

#### Scenario: Multi-seller order with different commission rates
- **WHEN** an order contains items from Seller A (15% rate) and Seller B (10% rate)
- **THEN** each line item uses its own seller's commission rate for calculation

#### Scenario: Line item commission attribution
- **WHEN** viewing an order with multiple line items from different sellers
- **THEN** each line item shows its own seller_id, commission_rate, commission_amount, and seller_amount

### Requirement: Commission rate configuration
The system SHALL support setting a commission rate per seller. Commission rates are percentages (e.g., 0.15 for 15%). Rates SHALL be configurable by administrators and apply only to future orders.

#### Scenario: Default commission rate on seller creation
- **WHEN** a new seller registers
- **THEN** the system assigns a default commission rate (e.g., 0.15) to their account

#### Scenario: Admin updates seller commission rate
- **WHEN** an administrator updates a seller's commission rate to 0.10
- **THEN** the seller's rate is updated and applies to all orders created after the update

#### Scenario: Commission rate validation
- **WHEN** an administrator attempts to set a commission rate outside valid range (e.g., 1.5 or -0.1)
- **THEN** the system rejects the update with 400 Bad Request error

### Requirement: Commission transparency
The system SHALL display commission information transparently on customer receipts and seller order views. Customers SHALL see the gross price they paid; sellers SHALL see their net amount after commission.

#### Scenario: Customer sees gross price on receipt
- **WHEN** a customer receives an order confirmation
- **THEN** the receipt shows the full purchase price they paid (e.g., $100 for a seller's product)

#### Scenario: Seller sees net amount on order
- **WHEN** a seller views an order
- **THEN** the order shows seller_gross_amount = $100, commission_amount = $15, seller_amount = $85

#### Scenario: Admin audit view includes commission breakdown
- **WHEN** an administrator views an order
- **THEN** the order displays full commission breakdown with rate, gross, commission, and seller amount for each line item

### Requirement: Commission validation and reconciliation
The system SHALL validate that all commission amounts are calculated correctly and audit-able. Historical commission records SHALL not be modifiable after order creation.

#### Scenario: Commission amount immutable after order creation
- **WHEN** trying to modify commission_amount on an order that has been created
- **THEN** the system rejects the update

#### Scenario: Commission reconciliation query
- **WHEN** running a report of all commissions collected in a period
- **THEN** the system returns accurate total commissions and per-seller breakdowns
