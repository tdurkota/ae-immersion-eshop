## Purpose

Manages the product catalog with support for both platform-owned and seller-owned products, enabling customers to browse a mixed marketplace of offerings from multiple sources.

## ADDED Requirements

### Requirement: Product catalog includes seller ownership
The system SHALL track product ownership. Each product SHALL have an optional seller_id field. Products with seller_id = NULL represent platform-owned products. Products with seller_id set represent third-party seller products. Both types SHALL coexist in the same catalog.

#### Scenario: Platform and seller products in same catalog
- **WHEN** a customer searches the catalog
- **THEN** results include both platform products (seller_id = NULL) and seller products (seller_id = UUID)

#### Scenario: Product detail includes seller attribution
- **WHEN** a customer views a product
- **THEN** the response includes seller_id and seller_name (if not platform product)

#### Scenario: Platform products identified
- **WHEN** viewing a platform-owned product
- **THEN** seller_id is NULL and seller_name is NULL or "Platform"

### Requirement: Catalog filtering by seller
The system SHALL support filtering products by seller. Customers SHALL be able to view all products from a specific seller or view only platform products.

#### Scenario: Filter by specific seller
- **WHEN** a customer requests GET /api/products?seller_id={sellerId}
- **THEN** the system returns only products from that seller

#### Scenario: Filter platform products only
- **WHEN** a customer requests GET /api/products?seller_id=platform
- **THEN** the system returns only platform-owned products (seller_id = NULL)

#### Scenario: No filter returns all products
- **WHEN** a customer searches without seller filter
- **THEN** results include both platform and all seller products

### Requirement: Seller profile visible on products
The system SHALL include seller information on product listings and detail pages. At minimum, product responses SHALL include seller_id, seller_name, and seller_status (Active/Inactive).

#### Scenario: Seller info in product list
- **WHEN** a customer browses product listing
- **THEN** each product card shows seller name or "Official Store" for platform products

#### Scenario: Seller info in product detail
- **WHEN** a customer views product detail page
- **THEN** page displays seller name, seller description (if available), and link to seller's other products

#### Scenario: Inactive seller products marked
- **WHEN** a seller account becomes inactive
- **THEN** their products still appear in catalog but show seller status as Inactive

### Requirement: Product availability based on seller status
The system MAY restrict new orders for products from inactive or suspended sellers. This behavior is deferred to the Orders service but SHALL be coordinated through product response data.

#### Scenario: Product from suspended seller still visible
- **WHEN** a seller account is suspended
- **THEN** their products remain visible in catalog with status indicator

#### Scenario: Availability coordination (deferred to Orders)
- **WHEN** a customer attempts to order from a suspended seller's product
- **THEN** the Orders service checks seller status and rejects the order (not Catalog responsibility)

### Requirement: Seller storefront endpoint
The system SHALL provide an endpoint to view all products from a specific seller. This endpoint SHALL support pagination and sorting.

#### Scenario: View seller storefront
- **WHEN** a customer requests GET /api/sellers/{sellerId}/storefront/products
- **THEN** the system returns all products from that seller with pagination

#### Scenario: Seller storefront pagination
- **WHEN** a seller has 150 products and customer requests page 2
- **THEN** the system returns the second page of results (20 products per page by default)

#### Scenario: Seller storefront sorting
- **WHEN** a customer requests GET /api/sellers/{sellerId}/storefront/products?sort=price&order=asc
- **THEN** products are sorted by price in ascending order
