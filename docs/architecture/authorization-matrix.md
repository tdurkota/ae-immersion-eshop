# Authorization Matrix

## Roles

- **Admin**
- **Seller**
- **Customer**
- **Support**

## Access Matrix

| Action | Admin | Seller | Customer | Support |
| --- | --- | --- | --- | --- |
| Register seller | Yes | Yes (self) | No | No |
| View own profile | Yes | Yes | N/A | N/A |
| View all sellers | Yes | No | No | No |
| Add product | Yes | Own only | No | No |
| View products | Yes | Own | All | All |
| Update/delete product | Yes | Own | No | No |
| Create order | No | No | Yes | No |
| View orders | Yes | Own | Own | All |
| View payouts | Yes | Own | No | Yes (read) |
| Process payout | Yes | No | No | Limited |

## JWT `seller_id` Claim Validation

- Tokens used for seller-scoped operations must include a trusted `seller_id` claim issued by the identity service.
- Services must validate the JWT signature, issuer, audience, expiration, and role claims before trusting `seller_id`.
- A seller may act only on resources whose `seller_id` matches the authenticated token claim.
- Admin flows may bypass seller ownership checks only when the action is explicitly admin-authorized.
- Requests with missing, malformed, or mismatched `seller_id` values must be rejected with an authorization failure rather than defaulting to broader access.

## Authorization Checks per Service

### Identity Service

- Issue role claims for `admin`, `seller`, `customer`, and `support`.
- Include `seller_id` only for authenticated seller principals that are mapped to a seller record.
- Prevent callers from self-assigning elevated roles or arbitrary `seller_id` values during registration or token refresh.

### Sellers Service

- Allow seller self-registration, while restricting broader seller listing and management operations to admins.
- Enforce that seller users can access only their own seller profile and related seller-owned records.
- Validate that admin-only seller visibility and management endpoints cannot be reached by customer or support roles.

### Catalog Service

- Allow customers and support users to view products according to the matrix.
- Require admin role or matching `seller_id` ownership for product creation, update, and delete operations.
- Reject seller attempts to modify products that belong to a different seller.

### Ordering Service

- Allow order creation only for customer principals.
- Restrict order visibility to admins, support, and the owning customer or seller based on the order's linked identities.
- Validate ownership on every order read to prevent cross-customer or cross-seller access.

### Payouts Service

- Allow payout visibility to admins, the owning seller, and support in read-only scenarios.
- Restrict payout processing to admins, with any support capability limited to explicitly defined operational actions only.
- Validate the requesting seller's `seller_id` against the payout record before returning seller-scoped payout data.
