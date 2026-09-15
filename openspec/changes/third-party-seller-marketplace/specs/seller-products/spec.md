## Purpose

Allows sellers to add, update, and manage their product listings in the marketplace catalog, with full control over their product inventory.

## ADDED Requirements

### Requirement: Seller can add products
The system SHALL allow authenticated sellers to add new products to the catalog. Sellers SHALL provide product name, description, price, category, and stock quantity. Each product SHALL be attributed to the seller who created it.

#### Scenario: Seller adds product successfully
- **WHEN** a seller submits POST /api/sellers/{id}/products with valid product data
- **THEN** the system creates the product, returns 201 Created, and the product appears in the marketplace attributed to that seller

#### Scenario: Seller adds product with missing fields
- **WHEN** a seller attempts to add a product with missing required fields (name, price, or category)
- **THEN** the system returns 400 Bad Request with error listing missing fields

#### Scenario: Seller adds product with invalid price
- **WHEN** a seller attempts to add a product with negative or zero price
- **THEN** the system returns 400 Bad Request with error "Price must be greater than zero"

#### Scenario: Suspended seller cannot add products
- **WHEN** a suspended seller attempts to add a product
- **THEN** the system returns 403 Forbidden with message "Your seller account is suspended"

### Requirement: Seller can update products
The system SHALL allow sellers to update product information they own. Sellers SHALL be able to modify name, description, price, category, and stock quantity. Updates SHALL be immediately reflected in the marketplace.

#### Scenario: Seller updates product successfully
- **WHEN** a seller submits PUT /api/sellers/{id}/products/{productId} with updated data
- **THEN** the system persists changes and returns 200 OK with updated product details

#### Scenario: Seller attempts to update another seller's product
- **WHEN** a seller attempts to update a product owned by another seller
- **THEN** the system returns 403 Forbidden

#### Scenario: Seller cannot change product owner
- **WHEN** a seller attempts to change the seller_id field of their product
- **THEN** the system ignores the seller_id update and returns success with original seller retained

### Requirement: Seller can delete products
The system SHALL allow sellers to delete their own products from the catalog. Deleted products SHALL no longer appear in search results or product listings. Historical references in completed orders SHALL remain unchanged for audit purposes.

#### Scenario: Seller deletes product successfully
- **WHEN** a seller submits DELETE /api/sellers/{id}/products/{productId}
- **THEN** the system removes the product from catalog and returns 204 No Content

#### Scenario: Seller deletes product with active orders
- **WHEN** a seller deletes a product that appears in open/processing orders
- **THEN** the system allows deletion; existing order references are preserved with product snapshot data

#### Scenario: Deleted product not visible in catalog
- **WHEN** a customer searches the catalog after a product is deleted
- **THEN** the product does not appear in search results or category listings

### Requirement: Products include seller attribution
The system SHALL attach seller information to every product in the catalog. Product responses SHALL include seller ID, seller name, and seller status. Customers SHALL be able to identify which seller offers each product.

#### Scenario: Product detail includes seller info
- **WHEN** a customer requests GET /api/products/{productId}
- **THEN** the response includes seller_id, seller_name, and seller_status fields

#### Scenario: Catalog search results include seller attribution
- **WHEN** a customer searches the catalog (GET /api/products?search=...)
- **THEN** each product in results includes seller_id and seller_name

#### Scenario: Products from suspended sellers still visible
- **WHEN** a seller account is suspended, their products remain in the catalog
- **THEN** product listings still show the seller info but new orders may be blocked (handled by order service)

### Requirement: Seller can view their products
The system SHALL provide an endpoint for sellers to list all products they have created. Sellers SHALL see complete product details including inventory levels, pricing, and sales statistics placeholder.

#### Scenario: Seller views their product list
- **WHEN** a seller requests GET /api/sellers/{id}/products with valid authentication
- **THEN** the system returns a list of all products created by that seller with full details

#### Scenario: Seller product list pagination
- **WHEN** a seller has more than 20 products and requests GET /api/sellers/{id}/products?page=2&pageSize=20
- **THEN** the system returns the second page of results with 20 products

#### Scenario: Seller product list for inactive account
- **WHEN** an inactive seller requests GET /api/sellers/{id}/products
- **THEN** the system returns their products; access control is still enforced
