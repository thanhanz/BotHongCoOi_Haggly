# Catalog Module Guide

## Current scope

Category, Product, and ProductStall are implemented Catalog API slices.
ProductStall stores a reusable stall-specific configuration for an existing
catalog Product; daily availability remains an Inventory concern.

## Entry points

- Application commands and queries: `src/Haggly.Application/Modules/Catalog`.
- Application ports: `src/Haggly.Application/Abstractions/Catalog`.
- EF configuration and command repository:
  `src/Haggly.Infrastructure/Persistence/Configurations/Catalog` and
  `Repositories/Catalog`.
- Dapper active Category, Product, and ProductStall reads: `Persistence/Queries/Catalog`.
- HTTP endpoints: `src/Haggly.Api/Endpoints/Catalog`.

## Category rules

- Creation requires a nonblank name and slug of at most 200 characters and a
  non-negative display order.
- Names are trimmed; slugs are trimmed and normalized to lowercase.
- New categories are `ACTIVE`.
- Active slugs are unique among non-deleted categories.
- A child category requires an existing active parent category.
- Reads expose active, non-deleted categories ordered by display order and name.

## Persistence

- Categories map to `catalog.categories`.
- `CreateCategories` creates the schema/table, a restricted self-parent foreign
  key, a filtered unique slug index for non-deleted rows, and read-order indexes.
- Products map to `catalog.products`. `CreateProducts` adds the restricted
  Category foreign key, filtered unique `CategoryId`/`Name` index, and
  Category/status read index.
- ProductStalls map to `catalog.product_stalls` through `ProductStallConfiguration`.
- `CreateProductStalls` creates the ProductStall table, restricted Product and
  Stall foreign keys, and the unique active `(StallId, ProductId)` index.

## Product rules

- Creation requires an existing active Category, a nonblank product name of at
  most 200 characters, and a defined `ProductUnit`.
- Names, descriptions, and image URLs are trimmed; new products are `ACTIVE`.
- Active product names are unique only within their Category.
- Product queries return active, non-deleted products; the list query supports
  an optional Category ID filter.

## API and authorization

- `POST /api/v1/categories` requires `CatalogContributor` (`VENDOR`,
  `MARKET_ADMIN`, or `PLATFORM_ADMIN`).
- `GET /api/v1/categories` and `GET /api/v1/categories/{id}` require an
  authenticated user.
- Category validation, conflict, and not-found exceptions map to Problem
  Details responses.
- `POST /api/v1/products` requires `CatalogContributor` (`VENDOR`,
  `MARKET_ADMIN`, or `PLATFORM_ADMIN`).
- `GET /api/v1/products` accepts an optional `categoryId`; it and
  `GET /api/v1/products/{id}` require an authenticated user.
- Product validation, conflict, and not-found exceptions map to Problem
  Details responses.
- `POST /api/v1/stalls/{stallId}/products` attaches an existing active Product
  and requires the authenticated user to own the Stall.
- `GET /api/v1/stalls/{stallId}/products` is paginated; the by-id GET reads one
  active association.
- `PATCH /api/v1/stalls/{stallId}/products/{id}` updates stall-specific fields
  and requires the Stall owner. There is intentionally no DELETE endpoint.
- ProductStall validation, forbidden, conflict, and not-found exceptions map to
  `400`, `403`, `409`, and `404` Problem Details responses respectively.

## Focused verification

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Catalog"
```

Catalog HTTP and PostgreSQL behavior belongs in the planned functional-test
suite. Do not replace those boundaries with mocks in `Haggly.UnitTests`.
