## Purpose

Tracks seller earnings and payout eligibility across orders, providing a complete audit trail of seller revenue and payment status.

## ADDED Requirements

### Requirement: Payout ledger creation
The system SHALL create a payout ledger entry for each order item sold by a seller. Each entry records seller_id, order_id, line_item_id, gross_amount, commission_amount, seller_amount, status, and timestamps. Ledger entries SHALL be immutable after creation.

#### Scenario: Payout entry created on order completion
- **WHEN** a customer completes purchase of a seller's product
- **THEN** the system creates a SellerPayout ledger entry with status "Pending" recording gross, commission, and seller amount

#### Scenario: Ledger entry for multi-item order
- **WHEN** a customer orders multiple items from a seller
- **THEN** the system creates one SellerPayout entry per item, not one consolidated entry

#### Scenario: Ledger entries for multi-seller order
- **WHEN** an order contains items from multiple sellers
- **THEN** the system creates separate SellerPayout entries for each seller's items

#### Scenario: Payout ledger immutable
- **WHEN** attempting to modify a SellerPayout entry after creation
- **THEN** the system rejects the modification and returns 403 Forbidden

### Requirement: Payout status tracking
The system SHALL track payout status for each ledger entry: Pending (earned but not yet processed), Processed (scheduled for payout), or Paid (successfully transferred to seller). Status transitions SHALL be recorded with timestamps.

#### Scenario: Payout status progression
- **WHEN** a seller's order is created, processed, and then marked for payment
- **THEN** the payout entry transitions: Pending → Processed → Paid

#### Scenario: Seller views payout status
- **WHEN** a seller requests GET /api/sellers/{id}/payouts
- **THEN** the system returns payout entries with status (Pending, Processed, Paid) and amounts

#### Scenario: Status update includes timestamp
- **WHEN** a payout status changes
- **THEN** the system records the status_changed_at timestamp for audit trail

### Requirement: Seller payout summary
The system SHALL provide summary endpoints showing seller's total earnings. Sellers SHALL see breakdown by status (total pending, total processed, total paid) and can view detailed payout history.

#### Scenario: Seller views payout summary
- **WHEN** a seller requests GET /api/sellers/{id}/payouts/summary
- **THEN** the system returns total_earned, total_pending, total_processed, total_paid

#### Scenario: Payout summary updates with new orders
- **WHEN** a new order is placed and processed
- **THEN** seller's total_earned increases immediately and appears in Pending category

#### Scenario: Payout summary shows historical totals
- **WHEN** a seller has completed payouts in previous periods
- **THEN** summary reflects cumulative total_paid and current pending/processed amounts

### Requirement: Payout filtering and reporting
The system SHALL support filtering payout entries by date range, status, and order ID. Sellers and administrators SHALL be able to generate reports of earnings.

#### Scenario: Seller filters payouts by status
- **WHEN** a seller requests GET /api/sellers/{id}/payouts?status=Paid
- **THEN** the system returns only payouts with status "Paid"

#### Scenario: Seller filters payouts by date range
- **WHEN** a seller requests GET /api/sellers/{id}/payouts?fromDate=2026-01-01&toDate=2026-01-31
- **THEN** the system returns payouts created within that date range

#### Scenario: Admin views all seller payouts
- **WHEN** an administrator requests GET /api/admin/payouts (admin endpoint)
- **THEN** the system returns payouts for all sellers with filtering options

### Requirement: Payout reconciliation
The system SHALL ensure payout ledger totals match order commission calculations. Platform commissions and seller amounts SHALL reconcile to actual order totals. Manual audit trails SHALL support compliance verification.

#### Scenario: Payout total reconciliation
- **WHEN** calculating total payouts across all sellers for a period
- **THEN** sum(seller_amount) + sum(commission_amount) = sum(gross_amount) for all orders in period

#### Scenario: Individual seller reconciliation
- **WHEN** totaling a seller's payouts and their order commissions
- **THEN** seller's total_earned = sum of all seller_amount entries for that seller's orders

#### Scenario: Audit trail completeness
- **WHEN** examining a seller's payout records
- **THEN** each entry includes order_id, line_item_id, and exact timestamps allowing full audit trail

### Requirement: Minimum payout threshold (optional MVP feature)
The system MAY optionally enforce a minimum payout amount threshold. Sellers with earned amounts below threshold remain in "Pending" status until threshold is met. This feature is deferred post-MVP.

#### Scenario: Seller below minimum payout threshold
- **WHEN** a seller has earned $5 but minimum payout is $100
- **THEN** payouts remain in Pending status until they accumulate to minimum

#### Scenario: Threshold configuration
- **WHEN** an administrator configures minimum payout threshold
- **THEN** the system applies the threshold to all sellers going forward
