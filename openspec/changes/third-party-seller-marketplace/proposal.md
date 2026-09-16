## Why

The eShop platform currently operates as a single-vendor catalog managed by the platform operator. To unlock new revenue streams, expand product selection, and build a competitive marketplace, we need to enable third-party sellers to list and sell products alongside platform-owned products. This transforms eShop from a curated catalog into an open hybrid marketplace, similar to Amazon or eBay, while maintaining the platform's direct sales channel.

## What Changes

- **Seller Registration & Accounts**: Third-party sellers can sign up, create accounts, and manage their seller profile
- **Seller Product Listing**: Sellers can add, edit, and remove their own products from the catalog
- **Product Attribution**: All products display seller information; customers can see and filter by seller
- **Order Attribution**: Orders track which seller fulfilled each item; sellers can view their own orders
- **Commission Tracking**: Platform captures a flat commission rate on seller orders and tracks payout eligibility per seller
- **Hybrid Catalog**: Both platform-owned and seller-owned products coexist in the product catalog and search results
- **Seller Authentication**: Sellers use dedicated accounts with role-based access to their own products and orders

## Capabilities

### New Capabilities

- `seller-management`: Seller registration, profile management, account status tracking
- `seller-products`: Sellers can list, manage, and remove their own products from the catalog
- `seller-orders`: Sellers can view orders containing their products and track fulfillment status
- `seller-commission`: Platform calculates and tracks seller commissions on orders
- `seller-payout-tracking`: Ledger of seller earnings and payout status per order

### Modified Capabilities

- `product-catalog`: Extend to support product ownership by sellers (add seller attribution, filtering)
- `orders`: Extend to track seller attribution on order items and store commission breakdown
- `customer-auth`: Extend identity system to support seller user accounts and role-based access

## Impact

- **Catalog Service**: Add seller reference to products, new endpoints for seller product management
- **Ordering Service**: Add seller tracking to order items, commission calculation and storage
- **Identity Service**: Support seller identity and role-based authorization
- **New Service**: Sellers.API for seller management, order visibility, and payout tracking
- **Database**: Add Seller table, SellerId field to CatalogItem, SellerId + CommissionRate to OrderLineItem, new SellerPayout ledger table
- **UI**: Product pages display seller info, new seller filter, seller profile pages, seller order management views
- **Event Bus**: Publish seller-related events (SellerRegistered, ProductListedBySeller, OrderCreatedWithSeller)
