## Purpose

Enables third-party users to register as sellers and manage their seller profile and account settings on the platform.

## ADDED Requirements

### Requirement: Seller registration
The system SHALL allow a new user to register as a seller by providing their email, name, and password. The registration process SHALL create a seller account linked to their user identity.

#### Scenario: Successful seller registration
- **WHEN** a new user submits a seller registration form with valid email, name, and password
- **THEN** the system creates a seller account, sends a confirmation email, and returns a 201 Created response

#### Scenario: Duplicate email registration
- **WHEN** a user attempts to register with an email already registered as a seller
- **THEN** the system returns a 409 Conflict error with message "Email already in use"

#### Scenario: Invalid input
- **WHEN** a user submits registration with missing required fields or invalid email format
- **THEN** the system returns a 400 Bad Request error with field-specific error messages

### Requirement: Seller profile management
The system SHALL allow sellers to view and update their profile information including name, description, contact information, and banking details for payouts. Only the seller who owns the account SHALL be able to update their profile.

#### Scenario: View own profile
- **WHEN** a seller requests GET /api/sellers/{id} with valid authentication
- **THEN** the system returns the seller's profile information including name, description, email, phone, and payout status

#### Scenario: Update profile
- **WHEN** a seller updates their profile with valid new information
- **THEN** the system persists the changes and returns the updated profile

#### Scenario: Unauthorized profile access
- **WHEN** a seller attempts to view or update another seller's profile
- **THEN** the system returns a 403 Forbidden error

### Requirement: Seller account status
The system SHALL track seller account status as Active, Inactive, or Suspended. Only administrators SHALL be able to change seller status. Sellers with Inactive or Suspended status SHALL not be able to list new products or receive new orders.

#### Scenario: Admin suspends seller
- **WHEN** an administrator suspends a seller account
- **THEN** the seller's status changes to Suspended and new product listings are rejected

#### Scenario: Suspended seller cannot list products
- **WHEN** a suspended seller attempts to add a product
- **THEN** the system returns a 403 Forbidden error with message "Seller account is suspended"

#### Scenario: Seller deactivates account
- **WHEN** a seller deactivates their account
- **THEN** the seller's status changes to Inactive and existing products remain visible but new listings are blocked

### Requirement: Seller authentication and authorization
The system SHALL issue JWT authentication tokens to sellers upon login. Seller tokens SHALL include a 'seller' role claim and the seller ID. All seller-specific endpoints SHALL verify the authentication token and validate that the seller can only access their own data.

#### Scenario: Seller login
- **WHEN** a seller logs in with valid email and password
- **THEN** the system returns a JWT token with 'seller' role and seller ID claims

#### Scenario: Seller accesses own data with valid token
- **WHEN** a seller includes valid JWT token in Authorization header when accessing GET /api/sellers/{id}
- **THEN** the system returns the seller's data

#### Scenario: Expired token rejection
- **WHEN** a seller uses an expired JWT token
- **THEN** the system returns a 401 Unauthorized error

### Requirement: Seller information retrieval
The system SHALL provide endpoints for retrieving seller information by ID and searching sellers by name. Public endpoints SHALL only return name, description, and rating/status; private endpoints accessible only to the account owner SHALL return full profile including banking details and payout history.

#### Scenario: Public seller profile view
- **WHEN** a customer requests GET /api/sellers/{id}/public
- **THEN** the system returns seller name, description, and account status (if Active)

#### Scenario: Private seller profile access
- **WHEN** a seller requests GET /api/sellers/{id} with valid authentication for that seller
- **THEN** the system returns full profile including email, phone, banking details, and payout information

#### Scenario: Search sellers by name
- **WHEN** a user searches sellers with query parameter GET /api/sellers?search=CompanyName
- **THEN** the system returns a list of sellers whose names match the search term
