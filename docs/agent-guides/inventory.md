# Inventory module guide

This guide records the Inventory slice that is currently implemented. It is
intentionally limited to verified repository behavior; reservations and order
coordination remain part of the later Sales workflow.

## Responsibilities

Inventory owns a vendor stall's daily session, product-listing snapshots,
stock quantities, availability/status transitions, price changes, and the
append-only inventory ledger. Catalog owns product identity and stall-product
configuration. Sales will own reservations and fulfillment.

## Layer map

- Domain: `src/Haggly.Domain/Modules/Inventory` contains
  `InventorySession`, `DailyProductListing`, `InventoryLedger`, and the
  inventory enums and state transitions.
- Application: `src/Haggly.Application/Modules/Inventory` contains commands,
  queries, handlers, DTOs, validation, exceptions, the business clock port,
  and Inventory persistence/reference-query ports.
- Infrastructure: `src/Haggly.Infrastructure/Persistence/Configurations/Inventory`
  contains the EF mappings; `Repositories/Inventory` contains write-side
  adapters; `Queries/Inventory/DapperInventoryQuery.cs` contains read-side
  session and ledger queries.
- API: `src/Haggly.Api/Endpoints/Inventory` exposes the vendor routes and keeps
  HTTP request/response mapping separate from business decisions.

## Verified business rules

- A stall has at most one session for a business date. The database also
  enforces the unique `(StallId, BusinessDate)` constraint.
- Business date comes from `IBusinessClock`, configured for
  `Asia/Ho_Chi_Minh` with the Windows time-zone fallback.
- Opening a listing snapshots product name and selling unit, initializes
  current/available quantities, and creates an opening ledger entry.
- `AvailableQuantity` is derived as `CurrentQuantity - ReservedQuantity`.
  Negative quantities and reservations above current stock are rejected.
- Listing mutations require the current `Version` as `expectedVersion`.
  `Version` starts at zero and is incremented by domain mutations; EF maps it
  as an optimistic-concurrency token.
- Price changes and quantity adjustments append ledger entries. Ledger rows
  are immutable and are normalized to inserts when appended to a tracked
  listing.
- Sessions transition from `OPEN` to `CLOSED`; closed sessions cannot be
  mutated.
- Application access checks require an active, non-deleted stall owned by the
  authenticated approved vendor, and active stall-product records for new
  listings.

## HTTP routes

All routes require the `VendorOnly` policy and are grouped under
`/api/v1/vendor/stalls/{stallId}`:

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/inventory-sessions/open` | Open today's session and optionally create listings |
| `GET` | `/inventory-sessions/current` | Read today's session |
| `GET` | `/inventory-sessions/previous` | Read the latest earlier session |
| `POST` | `/inventory-sessions/current/close` | Close today's session |
| `POST` | `/inventory-listings` | Add a listing to the open session |
| `PATCH` | `/inventory-listings/{listingId}` | Change public price or visibility |
| `POST` | `/inventory-adjustments` | Apply a signed stock adjustment |
| `GET` | `/inventory-ledger` | Filter and page ledger entries |

Application exceptions are translated by `ApiExceptionHandler` to the shared
Problem Details contract. Swagger includes the Inventory routes in the
Development document.

## Persistence and verification

Inventory persistence is introduced by the
`CreateInventoryEntities` migration. Quantity columns use `decimal(18,3)`;
prices use `decimal(18,2)`. EF handles writes and Dapper handles session and
ledger reads. Real boundary tests require PostgreSQL at the connection
configured by `HAGGLY_TEST_CONNECTION_STRING` or the repository's default
`localhost:5433` test database.

Focused checks:

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore
dotnet test tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Inventory"
```
