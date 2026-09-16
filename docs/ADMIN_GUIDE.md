# Admin Guide: Third-Party Seller Marketplace Management

This guide provides administrative procedures for managing the third-party seller marketplace in eShop, including seller suspension, verification, payout management, and dispute resolution.

## Table of Contents

1. [Overview](#overview)
2. [Seller Management](#seller-management)
3. [Payout Management](#payout-management)
4. [Monitoring and Alerts](#monitoring-and-alerts)
5. [Troubleshooting](#troubleshooting)

## Overview

The Admin console provides tools to:

- Monitor seller activity and compliance
- Suspend or restrict problematic sellers
- Review and process payout ledger entries
- Generate financial reports and reconciliation data
- Investigate disputes and chargebacks

### Admin Access

Admin users require:
- Valid Identity.API account with `admin` role
- JWT token with `role: "admin"` claim
- Elevated API key for administrative endpoints (POST/PUT/DELETE operations)

Get an admin token:

```bash
curl -X POST https://localhost:5000/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin@example.com",
    "password": "secure-admin-password",
    "role": "admin"
  }'
```

## Seller Management

### View All Sellers

List all registered sellers with filtering options:

```bash
curl -X GET "https://localhost:5001/api/admin/sellers?status=Active&page=1&pageSize=50" \
  -H "Authorization: Bearer <admin-token>"
```

**Query Parameters:**
- `status`: Filter by seller status (Active, Suspended, Pending)
- `page`: Page number (default: 1)
- `pageSize`: Results per page (default: 20, max: 100)
- `search`: Search by seller name or email
- `registeredAfter`: ISO 8601 date to filter by registration date

**Response:**
```json
{
  "sellers": [
    {
      "sellerId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Premium Electronics",
      "email": "seller@example.com",
      "status": "Active",
      "commissionRate": 0.15,
      "createdAt": "2026-09-16T12:00:00Z",
      "productCount": 42,
      "totalSalesAmount": 5420.50,
      "pendingPayoutAmount": 427.50,
      "complianceFlags": 0
    }
  ],
  "page": 1,
  "pageSize": 50,
  "total": 127
}
```

### View Seller Details

Get comprehensive information about a specific seller:

```bash
curl -X GET "https://localhost:5001/api/admin/sellers/550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer <admin-token>"
```

**Response includes:**
- Full seller profile
- Activity metrics (products, orders, revenue)
- Compliance status and any active flags
- Recent orders and payout history
- Communication history

### Suspend a Seller

Suspend a seller to prevent them from creating new orders and listings:

```bash
curl -X PATCH "https://localhost:5001/api/admin/sellers/550e8400-e29b-41d4-a716-446655440000/suspend" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "reason": "Policy violation: counterfeit goods",
    "effectiveImmediately": true,
    "notificationTemplate": "suspension_notice"
  }'
```

**Suspension Effects:**
- Seller cannot create new product listings
- Seller cannot accept new orders (API returns 403)
- Existing orders and shipments continue as normal
- Seller retains access to view historical data

**Response:**
```json
{
  "sellerId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Suspended",
  "suspensionReason": "Policy violation: counterfeit goods",
  "suspendedAt": "2026-09-16T14:30:00Z",
  "suspendedBy": "admin@example.com",
  "appealDeadline": "2026-09-23T14:30:00Z"
}
```

**Notification:** Seller receives email notification via the specified template.

### Reinstate a Seller

Reinstate a previously suspended seller:

```bash
curl -X PATCH "https://localhost:5001/api/admin/sellers/550e8400-e29b-41d4-a716-446655440000/reinstate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "reason": "Seller completed required training and remediation"
  }'
```

**Effect:** Seller status changes back to Active; seller can immediately accept new orders.

### Update Commission Rate

Adjust a seller's commission rate (takes effect on future orders):

```bash
curl -X PATCH "https://localhost:5001/api/admin/sellers/550e8400-e29b-41d4-a716-446655440000/commission" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "newCommissionRate": 0.20,
    "effectiveDate": "2026-10-01T00:00:00Z",
    "reason": "Premium seller tier upgrade"
  }'
```

**Important:**
- Existing orders retain their historical commission rates
- New rate applies only to orders created after `effectiveDate`
- Rate changes are logged for audit trail

## Payout Management

### View All Payouts

List all payout ledger entries with filtering:

```bash
curl -X GET "https://localhost:5001/api/admin/payouts?status=Pending&page=1&pageSize=50" \
  -H "Authorization: Bearer <admin-token>"
```

**Query Parameters:**
- `status`: Filter by status (Pending, Processed, Paid)
- `sellerId`: Filter by seller
- `fromDate`: Start date (ISO 8601)
- `toDate`: End date (ISO 8601)
- `orderId`: Filter by order ID
- `page`, `pageSize`: Pagination

**Response:**
```json
{
  "payouts": [
    {
      "payoutId": "payout-1",
      "sellerId": "550e8400-e29b-41d4-a716-446655440000",
      "sellerName": "Premium Electronics",
      "orderId": "order-12345",
      "lineItemId": "lineitem-1",
      "grossAmount": 100.00,
      "commissionAmount": 15.00,
      "sellerAmount": 85.00,
      "status": "Pending",
      "createdAt": "2026-09-16T12:30:00Z",
      "paidAt": null
    }
  ],
  "page": 1,
  "pageSize": 50,
  "total": 1247
}
```

### Generate Payout Report

Generate a comprehensive payout report for a date range:

```bash
curl -X GET "https://localhost:5001/api/admin/payouts/report?fromDate=2026-09-01&toDate=2026-09-30" \
  -H "Authorization: Bearer <admin-token>"
```

**Response:**
```json
{
  "period": {
    "start": "2026-09-01T00:00:00Z",
    "end": "2026-09-30T23:59:59Z"
  },
  "summary": {
    "totalGrossAmount": 54250.00,
    "totalCommissionAmount": 8137.50,
    "totalSellerAmount": 46112.50,
    "totalPayoutsProcessed": 432,
    "totalPayoutsPending": 28,
    "totalPayoutsPaid": 404
  },
  "byStatus": {
    "pending": {
      "count": 28,
      "totalAmount": 2150.00
    },
    "processed": {
      "count": 15,
      "totalAmount": 1200.00
    },
    "paid": {
      "count": 404,
      "totalAmount": 42762.50
    }
  },
  "topSellers": [
    {
      "sellerId": "...",
      "sellerName": "Premium Electronics",
      "totalEarnings": 5420.50,
      "payoutCount": 45
    }
  ],
  "reconciliation": {
    "status": "balanced",
    "discrepancies": []
  }
}
```

### Process Payouts

Mark payouts as ready for payment processing:

```bash
curl -X POST "https://localhost:5001/api/admin/payouts/process" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "payoutIds": ["payout-1", "payout-2", "payout-3"],
    "batchId": "batch-20260916-001",
    "notes": "Weekly payout run"
  }'
```

**Effect:** Selected payouts transition from "Pending" → "Processed" and are added to the payment batch queue.

**Response:**
```json
{
  "batchId": "batch-20260916-001",
  "processedCount": 3,
  "totalAmount": 1275.00,
  "status": "queued_for_payment",
  "estimatedPaymentDate": "2026-09-20T00:00:00Z"
}
```

### Mark Payouts as Paid

Record that payouts have been successfully transferred to seller bank accounts:

```bash
curl -X POST "https://localhost:5001/api/admin/payouts/mark-paid" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "batchId": "batch-20260916-001",
    "paymentDate": "2026-09-20T10:30:00Z",
    "paymentReference": "ACH-transfer-ref-12345",
    "notes": "Successfully transferred to seller bank accounts"
  }'
```

**Effect:** Payouts in the batch transition from "Processed" → "Paid" and are marked with payment date/reference for audit trail.

### Reconciliation Query

Verify payout ledger integrity and identify discrepancies:

```bash
curl -X GET "https://localhost:5001/api/admin/payouts/reconcile?date=2026-09-16" \
  -H "Authorization: Bearer <admin-token>"
```

**Validation checks:**
- Sum of all payouts equals corresponding orders
- Commission rates are consistent
- No missing payout entries for completed orders
- Status transitions are logical

**Response:**
```json
{
  "reconciliationDate": "2026-09-16T00:00:00Z",
  "status": "balanced",
  "stats": {
    "ordersProcessed": 150,
    "payoutEntriesCreated": 180,
    "discrepancies": []
  },
  "discrepancies": [
    {
      "type": "missing_payout_entry",
      "orderId": "order-99999",
      "lineItemId": "lineitem-5",
      "impact": "High",
      "recommendation": "Manually create payout entry or investigate order processing failure"
    }
  ]
}
```

If discrepancies are found, investigate the underlying order and create manual payout entries as needed.

## Monitoring and Alerts

### Key Metrics to Monitor

**Seller Health Metrics:**
- New seller registrations (daily/weekly)
- Active sellers (listing products)
- Seller suspension rate
- Average commission rate by tier

**Financial Metrics:**
- Total gross sales
- Total commissions collected
- Pending payout amount (should not grow indefinitely)
- Average payout cycle time (Pending → Paid)
- Discrepancies in reconciliation

**Order Metrics:**
- Multi-seller orders (should increase over time)
- Average order value with seller products
- Returns/cancellations rate by seller
- Customer satisfaction scores by seller

### Alert Thresholds

Configure alerts for:

1. **High Payout Pending Amount**
   - If `totalPending > $50,000` AND pending items > 100
   - Action: Trigger immediate payout processing

2. **Seller Compliance Flags**
   - Excessive returns (>5% of seller's orders)
   - Multiple customer complaints (>3 in 7 days)
   - Chargebacks (>2% of transactions)
   - Action: Review and consider suspension

3. **Data Integrity Issues**
   - Reconciliation discrepancies found
   - Missing payout entries
   - Commission calculation errors
   - Action: Investigate and resolve immediately

4. **Performance Degradation**
   - API response times > 2 seconds
   - Event handler failures (unprocessed orders)
   - Database query timeouts
   - Action: Scale services and investigate bottlenecks

### Setting Up Alerts

Alerts can be configured via your monitoring system (Application Insights, Datadog, New Relic):

```
# Example: Alert if pending payouts exceed threshold
IF metric:pending_payouts_amount > 50000
  AND metric:pending_payout_count > 100
  FOR 1 hour
THEN notify(admin-channel)
```

## Troubleshooting

### Seller Cannot Accept Orders

**Symptoms:** Orders from seller's products return 403 Forbidden or are not created.

**Investigation:**
1. Check seller status: `GET /api/admin/sellers/{sellerId}`
   - If `status: "Suspended"`, reinstate seller or address underlying issue
2. Check seller's product status
   - Products may have `isActive: false`
   - Reactivate products if appropriate
3. Check order service logs for rejection reason

**Resolution:**
- Reinstate seller (if wrongfully suspended)
- Update seller status to Active
- Reactivate seller's products

### Payout Entry Not Created for Order

**Symptoms:** Order completed but no payout entry in ledger.

**Investigation:**
1. Check if order includes seller products
   - Query order details: `GET /api/orders/{orderId}`
   - Verify line items have `sellerId` field
2. Check Sellers.API event handler logs
   - Look for `OrderCreatedIntegrationEvent` processing errors
   - Check RabbitMQ dead-letter queue for failed messages
3. Verify seller exists and is not suspended

**Resolution:**
- If event processing failed: manually create payout entry via admin API
- If seller was suspended: reactive seller first, then create payout
- Retry event processing if infrastructure issue is resolved

### Commission Rate Mismatch

**Symptoms:** Payout amounts don't match calculated commissions.

**Investigation:**
1. Verify commission rate stored on order line item
   - Commission is captured at order time, not read from current seller record
   - Line item rate may differ from current seller rate
2. Check reconciliation query: `GET /api/admin/payouts/reconcile`
3. Review audit log of commission rate changes

**Resolution:**
- Commission rates are immutable for past orders (by design)
- If mistake occurred: create adjustment payout entry
- Use admin API to adjust seller's current rate prospectively

### Payout Stuck in "Processed"

**Symptoms:** Payouts marked as Processed but never transition to Paid.

**Investigation:**
1. Check batch status in payment processor integration
2. Verify ACH transfer initiation logs
3. Contact payment processor to confirm transfer status

**Resolution:**
- If payment succeeded but status not updated: manually mark as paid
- If payment failed: troubleshoot with payment processor, retry batch
- If scheduled for future date: confirm scheduled payment is still queued

## Support

For complex issues or escalations:

- **Compliance Questions:** Contact compliance@example.com
- **Technical Issues:** Create issue in internal tracking system
- **Payment Processor Issues:** Contact payment processor support team

Remember: All admin actions are logged for audit trail compliance.
