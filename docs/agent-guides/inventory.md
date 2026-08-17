# Inventory module guide

This guide records the continuous Inventory slice implemented in the current
workspace. Reservations and online-order coordination remain deferred to the
Sales workflow.

## Responsibilities

Inventory owns one stock aggregate per stall, its InventoryItems, quantity
invariants, optimistic concurrency, and the append-only quantity ledger.
Catalog owns ProductStall selling unit, current price, activation, negotiation,
and product identity. Sales stores immutable name, unit, and price snapshots at
the time of a completed sale.

## Layer map

- Domain: `src/Haggly.Domain/Modules/Inventory` contains `Inventory`,
  `InventoryItem`, `InventoryLedger`, and reservation types.
- Application: `src/Haggly.Application/Modules/Inventory` contains add, read,
  adjustment, and ledger use cases plus ownership checks and persistence ports.
- Infrastructure: EF configurations and repositories are under
  `Persistence/Configurations/Inventory` and `Repositories/Inventory`; Dapper
  reads are in `Queries/Inventory/DapperInventoryQuery.cs`.
- API: `src/Haggly.Api/Endpoints/Inventory` exposes vendor-only continuous
  inventory routes.

## Verified business rules

- Stall creation also creates its Inventory in the same EF unit of work.
- The database enforces one Inventory per Stall and one InventoryItem per
  ProductStall.
- InventoryItem stores `CurrentQuantity` and `ReservedQuantity`.
  `AvailableQuantity` is calculated as current minus reserved and is not stored.
- Quantities cannot be negative and reserved quantity cannot exceed current
  quantity. Adjustments cannot reduce current quantity below reserved stock.
- InventoryItem `Version` is an EF concurrency token and increments on quantity
  mutations.
- ProductStall owns `SellingUnit`, `CurrentUnitPrice`, and its own concurrency
  `Version`. POS submission checks both expected versions before snapshotting
  price/unit/name and deducting stock.
- Quantity changes append immutable InventoryLedger rows. Current product data
  is not duplicated in InventoryItem.
- `IInventorySaleRecorder` is the Sales-facing port. It verifies stall ownership,
  checks both InventoryItem and ProductStall versions, snapshots current catalog
  data, and records `POS_SALE` atomically with the sale.

## HTTP routes

All routes require `VendorOnly` and use
`/api/v1/vendor/stalls/{stallId}/inventory`:

| Method | Suffix | Purpose |
|---|---|---|
| `GET` | `` | Read inventory and items |
| `POST` | `/items` | Add an item with current quantity |
| `GET` | `/items/{inventoryItemId}` | Read one item |
| `POST` | `/adjustments` | Apply a signed adjustment |
| `GET` | `/ledger` | Filter and page quantity history |

## Persistence and verification

`RefactorContinuousInventory` backfills one Inventory per existing Stall,
selects the latest listing for each stall product as current InventoryItem
state, remaps historical ledgers, and moves latest current price to
ProductStall. Because daily records are consolidated, rollback requires a
pre-migration database backup.

Focused commands:

```powershell
dotnet build tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore
dotnet build tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-restore
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-build
dotnet test tests\Haggly.IntegrationTests\Haggly.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Inventory"
```
