# Architecture Decision: Nullable SellerId on CatalogItem

**Date:** 2026-09-15  
**Status:** Decided

## Context

The marketplace catalog must represent two kinds of products in the same browsing and ordering experience: products owned directly by the platform and products owned by third-party sellers. The system therefore needs a simple way to express product ownership without fragmenting the core catalog model or forcing separate product pipelines for first-party and marketplace inventory.

CatalogItem is the natural place to capture ownership because it is the canonical record for a sellable product in the catalog domain. The key question is how to model ownership so that the platform can continue supporting existing products while also introducing seller-owned products through Sellers.API and the shared database strategy chosen for the MVP.

We needed a representation that keeps reads straightforward, does not break existing catalog rows, and communicates clear semantics to application code and reporting queries.

## Decision

We will add an optional `seller_id` column to `CatalogItem`. The column will be a GUID and nullable.

When `seller_id` has a value, the catalog item is owned by the referenced seller. When `seller_id` is `NULL`, the catalog item is owned by the platform itself.

## Rationale

This decision is the simplest backward-compatible extension to the current schema. Existing platform-owned products can remain unchanged because their ownership is represented naturally by a `NULL` value. No synthetic migration is required to invent seller records for products that were never associated with third-party sellers.

The design also keeps ownership semantics easy to understand. Application code can distinguish platform inventory from seller inventory with one field and one rule: `NULL` means platform, non-`NULL` means seller-owned. That is more direct than introducing an additional ownership type column or an indirection table for a case where most queries simply need to know whether a seller relationship exists.

Operationally, the schema remains compact and efficient. Catalog reads, search queries, and order enrichment can continue using the primary catalog table without always joining to another structure just to classify ownership. That matters for the MVP, where simplicity and low-friction delivery are more valuable than modeling every ownership concept as a separate relational construct.

## Alternatives Considered

### Separate ownership mapping table

We considered storing seller ownership in a separate table keyed by catalog item. This was rejected because it adds an extra join to common queries and complicates writes for limited benefit. For the MVP, ownership is a simple optional relationship, not a rich subdomain that requires its own table.

### Special "platform" seller account

We considered creating a reserved seller record to represent platform-owned products. This was rejected because it obscures the meaning of platform ownership, introduces a fake business entity into the seller domain, and creates unnecessary coupling between catalog seed data and seller lifecycle rules.

## Consequences

The main positive consequence is that mixed catalogs are easy to query: one table can hold both platform and seller-owned products, and a single query can retrieve all products with ownership implied by `seller_id`.

The main risk is subtle `NULL` semantics. Developers must consistently remember that `NULL` is meaningful and intentional here, not missing data. Query logic, filters, and ORM mappings must preserve that distinction to avoid accidental misclassification of platform-owned products.

## Related Decisions

- **ARD 1:** Separate Sellers.API Microservice
- **ARD 2:** Single PostgreSQL Database for MVP
