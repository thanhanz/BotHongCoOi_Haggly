# Catalog Module Guide

## Current scope

Category is the only implemented Catalog vertical slice. Product and
ProductStall remain Domain-only scaffolds and must not be added to a Category
migration.

## Entry points

- Application commands and queries: `src/Haggly.Application/Modules/Catalog`.
- Application ports: `src/Haggly.Application/Abstractions/Catalog`.
- EF configuration and command repository:
  `src/Haggly.Infrastructure/Persistence/Configurations/Catalog` and
  `Repositories/Catalog`.
- Dapper active-category reads: `Persistence/Queries/Catalog`.
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
- `HagglyDbContext` explicitly ignores Product and ProductStall until their own
  persistence slice is implemented.

## API and authorization

- `POST /api/v1/categories` requires `CatalogContributor` (`VENDOR`,
  `MARKET_ADMIN`, or `PLATFORM_ADMIN`).
- `GET /api/v1/categories` and `GET /api/v1/categories/{id}` require an
  authenticated user.
- Category validation, conflict, and not-found exceptions map to Problem
  Details responses.

## Focused verification

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Category"
dotnet test tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Category"
```
