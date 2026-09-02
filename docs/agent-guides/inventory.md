# Inventory module guide

This guide records the continuous Inventory slice implemented in the current
workspace. Buyer cart/order coordination is implemented in Sales. Starting a
Payment now creates an aggregate stock hold without a separate reservation entity.

## Responsibilities

Inventory owns one stock aggregate per stall, its InventoryItems, quantity
invariants, optimistic concurrency, and the append-only quantity ledger.
Catalog owns ProductStall selling unit, current price, activation, negotiation,
and product identity. Sales stores immutable name, unit, and price snapshots at
the time of a completed sale.

## Layer map

- Domain: `src/Haggly.Domain/Modules/Inventory` contains `Inventory`,
  `InventoryItem`, and `InventoryLedger`.
- Application: `src/Haggly.Application/Modules/Inventory` contains add, read,
  adjustment, and ledger use cases plus ownership checks and persistence ports.
- Infrastructure: EF configurations and repositories are under
  `Persistence/Configurations/Inventory` and `Repositories/Inventory`; Dapper
  reads are in `Queries/Inventory/DapperInventoryQuery.cs`.
- API: `src/Haggly.Api/Endpoints/Inventory` exposes vendor-only continuous
  inventory routes.

Payment start reserves active OrderItem quantities. Successful online payments
consume those quantities and append idempotent online-sale ledger entries.

## Verified business rules

- Stall creation also creates its Inventory in the same EF unit of work.
- The database enforces one Inventory per Stall and one InventoryItem per
  ProductStall.
- InventoryItem stores `CurrentQuantity` and `ReservedQuantity`.
  `AvailableQuantity` is calculated as current minus reserved and is not stored.
- Quantities cannot be negative and reserved quantity cannot exceed current
  quantity. Adjustments cannot reduce current quantity below reserved stock.
- `StartPaymentHandler` calls `IInventoryPaymentRepository.ReserveAsync` inside
  the same PostgreSQL transaction that creates the Payment and request outbox
  row. The operation is all-or-nothing and uses InventoryItem concurrency tokens.
- A definitive provider decline publishes `PaymentFailedEvent`. The Inventory
  failure handler atomically inserts an InboxMessage and releases the active
  OrderItem quantities in one PostgreSQL transaction. Technical provider or
  consumer exceptions leave the quantities reserved for broker retry.
- InventoryItem `Version` is an EF concurrency token and increments on quantity
  mutations.
- ProductStall owns `SellingUnit`, `CurrentUnitPrice`, and its own concurrency
  `Version`. POS submission checks both expected versions before snapshotting
  price/unit/name and deducting stock.
- Quantity changes append immutable InventoryLedger rows. Current product data
  is not duplicated in InventoryItem.
- `InventoryPaymentSucceededHandler` loads active OrderItems, decreases both
  current and reserved quantity, and appends one `ONLINE_SALE` ledger row per
  item. The
  payment transaction is the ledger reference, and a filtered unique index
  prevents duplicate delivery from deducting the same item twice.
- `InventoryPaymentSucceededConsumer` owns the durable
  `inventory-payment-succeeded-v1` queue bound to the shared
  `payments.payment-succeeded.v1` exchange. Technical failures retry after
  1, 5, and 15 seconds before MassTransit error transport.
- `InventoryPaymentFailedConsumer` owns the durable
  `inventory-payment-failed-v1` queue bound to `payments.payment-failed.v1`.
  It retries technical failures after 1, 5, and 15 seconds and retains the
  original message in MassTransit's default `_error` queue after exhaustion.
- There is no InventoryReservation entity or reservation ledger entry. Active
  OrderItems supply the held quantities; `ReservedQuantity` is their aggregate
  while payment is pending or processing.
- `IInventorySaleRecorder` is the Sales-facing port. It verifies stall ownership,
  checks both InventoryItem and ProductStall versions, snapshots current catalog
  data, and records `POS_SALE` atomically with the sale.
- Sales cart commands use the read-only `ICartCatalog` port to compare requested
  quantity with `CurrentQuantity - ReservedQuantity`. Cart add/update/checkout
  do not reserve or deduct stock; checkout repeats the availability check before
  creating an Order.

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
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-build --filter "FullyQualifiedName~Inventory"
```

Inventory PostgreSQL and API behavior belongs in `Haggly.FunctionalTests` after
that project is introduced. Do not claim those boundaries were verified by the
unit suite.
