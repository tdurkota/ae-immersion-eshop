## Purpose

Extends authentication and authorization to support seller accounts with role-based access control, enabling sellers to manage products and view orders while maintaining customer and administrator privileges.

## ADDED Requirements

### Requirement: Seller role and identity
The system SHALL extend identity management to support seller user accounts. Sellers SHALL be a distinct user role separate from customers and administrators. Seller tokens SHALL include 'seller' role claim and seller_id.

#### Scenario: Seller registration creates seller identity
- **WHEN** a new user registers as a seller
- **THEN** the identity system creates a seller user account with seller role

#### Scenario: Seller JWT token includes seller role
- **WHEN** a seller logs in
- **THEN** the JWT token includes claim: role = 'seller' and seller_id = <UUID>

#### Scenario: Customer and seller are separate identities
- **WHEN** the same email address registers as both customer and seller
- **THEN** the system treats them as separate accounts with different credentials and access rights

### Requirement: Role-based access control
The system SHALL enforce role-based access control (RBAC) on seller-specific endpoints. Endpoints for seller management, product management, and order viewing SHALL only be accessible with seller role.

#### Scenario: Customer cannot access seller endpoints
- **WHEN** a customer attempts POST /api/sellers/{id}/products (add product)
- **THEN** the system returns 403 Forbidden (seller role required)

#### Scenario: Seller can access their own endpoints
- **WHEN** a seller with valid token requests GET /api/sellers/{id}/products
- **THEN** the system returns their products (seller_id matches token claim)

#### Scenario: Seller cannot access other seller's endpoints
- **WHEN** a seller attempts to access GET /api/sellers/{otherId}/products
- **THEN** the system returns 403 Forbidden (not authorized for other seller)

### Requirement: Seller data isolation
The system SHALL ensure sellers can only view and modify their own data. Sellers SHALL NOT be able to:
- View other sellers' products, orders, or earnings
- Modify other sellers' account information
- Access administrative functions

#### Scenario: Seller isolated to own data
- **WHEN** a seller requests GET /api/sellers/{id}/payouts for a different seller
- **THEN** the system returns 403 Forbidden

#### Scenario: Seller can view own orders
- **WHEN** a seller requests GET /api/sellers/{myId}/orders with their own seller_id
- **THEN** the system returns their orders

#### Scenario: Seller cannot view admin functions
- **WHEN** a seller attempts to access GET /api/admin/sellers
- **THEN** the system returns 403 Forbidden (admin role required)

### Requirement: Multiple user roles coexistence
The system SHALL support users with multiple roles or role transitions. A user can be a customer, seller, and administrator simultaneously or transition between roles.

#### Scenario: User is both customer and seller
- **WHEN** a seller makes a purchase
- **THEN** they are treated as customer for that purchase; seller role doesn't affect checkout

#### Scenario: Separate tokens per role
- **WHEN** a user with multiple roles logs in
- **THEN** they receive a token with all applicable roles

#### Scenario: Admin can impersonate seller for support
- **WHEN** an administrator needs to view a seller's data
- **THEN** system logs the admin access separately without exposing admin credentials to seller APIs

### Requirement: Token expiration and refresh
The system SHALL issue time-limited JWT tokens for sellers. Tokens SHALL expire after configurable duration (e.g., 1 hour). Sellers SHALL be able to refresh tokens.

#### Scenario: Seller token expiration
- **WHEN** a seller token expires
- **THEN** subsequent API requests return 401 Unauthorized

#### Scenario: Seller token refresh
- **WHEN** a seller requests token refresh with valid refresh token
- **THEN** the system returns a new JWT token with same role and seller_id

#### Scenario: Refresh token validation
- **WHEN** a seller attempts to refresh with an expired refresh token
- **THEN** the system returns 401 Unauthorized and requires re-authentication

### Requirement: Audit logging of seller access
The system SHALL log all significant seller API actions including login, product modifications, and order access for compliance and security purposes. Logs SHALL include timestamp, seller_id, action, and result.

#### Scenario: Seller login logged
- **WHEN** a seller logs in successfully
- **THEN** audit log records seller_id, login timestamp, IP address (optional)

#### Scenario: Product modification logged
- **WHEN** a seller adds, updates, or deletes a product
- **THEN** audit log records seller_id, action (add/update/delete), product_id, and timestamp

#### Scenario: Order access logged
- **WHEN** a seller views their orders
- **THEN** audit log records seller_id, order_id, and access timestamp
