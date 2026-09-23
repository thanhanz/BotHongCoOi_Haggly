# Marketplace Search

## Public contract

`GET /api/v1/search` is anonymous and accepts `q`, `stallPage`,
`stallPageSize`, `productPage`, and `productPageSize`. Stall and product results
are paged independently. Default page sizes are 5 stalls and 20 products; the
maximums are 20 and 100 respectively.

Queries contain between 2 and 100 searchable characters after Vietnamese name
normalization. Matching is case- and accent-insensitive and ranks exact matches
before prefixes and contains matches.

## Result eligibility

Stall results expose buyer-safe fields. An exact normalized stall-name match
includes up to eight purchasable product previews and the stall's total
available product count. The existing paged product-listing route remains the
way to retrieve all products for a stall.

Direct product results match the catalog product name or the stall-specific
display name. One result is returned per stall offer because price, unit,
negotiability, and availability belong to that offer. A purchasable result
requires an active, non-deleted product, ProductStall, and stall plus positive
available inventory (`CurrentQuantity - ReservedQuantity > 0`).

## Implementation ownership

Application validation and orchestration live in the Discovery module behind
`IMarketplaceSearchReader`. `DapperMarketplaceSearchRepository` owns the
cross-module PostgreSQL read model. The API endpoint performs transport mapping
only. PostgreSQL `unaccent` and `pg_trgm` support normalized matching and
expression indexes.
