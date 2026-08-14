# Daily Inventory API Implementation Plan

This document expands section 4 of `PLAN.md` into an implementation plan for
daily inventory at each vendor-owned stall. It is proposed behavior, not proof
that the described Application, Infrastructure, or API code already exists.

## Scope and implementation baseline

Inventory owns the daily, sellable state of a stall product. Catalog's
`ProductStall` remains the reusable stall-product configuration and supplies
the default price, display name, selling unit, and negotiation setting. A
`DailyProductListing` is a snapshot for one `InventorySession`; later POS and
online-order slices must consume that listing rather than mutate
`ProductStall` quantities.

The repository currently contains `InventorySession`, `DailyProductListing`,
`InventoryLedger`, and `InventoryReservation` Domain scaffolds, but no
Inventory Application use cases, persistence mappings, migrations, or API
endpoints. This slice should map and implement sessions, listings, and ledger
entries only. Reservation behavior remains part of the order workflow and must
not be partially implemented here.

## Proposed HTTP contract

```http
POST /api/v1/vendor/stalls/{stallId}/inventory-sessions/open
GET  /api/v1/vendor/stalls/{stallId}/inventory-sessions/current
GET  /api/v1/vendor/stalls/{stallId}/inventory-sessions/previous
POST /api/v1/vendor/stalls/{stallId}/inventory-sessions/current/close

POST  /api/v1/vendor/stalls/{stallId}/inventory-listings
PATCH /api/v1/vendor/stalls/{stallId}/inventory-listings/{listingId}
POST  /api/v1/vendor/stalls/{stallId}/inventory-adjustments
GET   /api/v1/vendor/stalls/{stallId}/inventory-ledger
```

All routes require `VendorOnly`. The authenticated subject is the actor and
must own `stallId`; neither vendor ID nor actor ID is accepted from a request
body. Nesting every resource under the stall makes the ownership boundary
explicit and prevents a listing ID from being used across stalls.

Opening a session accepts optional notes and an initial collection like:

```json
{
  "notes": "Morning stock count",
  "listings": [
    {
      "productStallId": "00000000-0000-0000-0000-000000000000",
      "openingQuantity": 25.5,
      "publicUnitPrice": 45000
    }
  ]
}
```

The server derives `BusinessDate`, `OpenedAt`, and `OpenedBy`. When price is
omitted, the current `ProductStall.DefaultUnitPrice` is copied. Product name
and selling unit are always copied as snapshots. The add-listing endpoint uses
the same item contract so a vendor can start with an empty session or add stock
later without reopening the day. `current` returns today's session even after
it is closed; `previous` returns the most recent session from an earlier
business date.

The listing patch accepts only `publicUnitPrice`, `status`, and
`expectedVersion`. It must not accept opening, current, reserved, or available
quantity. The adjustment request accepts `listingId`, a non-zero signed
`quantityDelta`, a required reason, and `expectedVersion`. The ledger endpoint
supports `businessDate`, `listingId`, `transactionType`, `page`, and
`pageSize`, with stable descending ordering by occurrence time and ID.

## Domain rules and decisions

- There is at most one session per `(StallId, BusinessDate)`. Opening an
  already-created business day returns `409 Conflict`; concurrent opens are
  also stopped by a database unique constraint.
- Business date is calculated from `TimeProvider` in one configured MVP market
  time zone, initially `Asia/Ho_Chi_Minh`, never from a client-supplied date or
  directly from the UTC calendar date. A future per-market time zone can
  replace this without changing the HTTP contract.
- Only an active, non-deleted stall owned by an approved, active vendor can
  open or mutate a session. Only active, non-deleted `ProductStall` records
  belonging to that stall can become listings.
- One product-stall can occur only once in a session. A session may open empty
  because the add-listing endpoint supports declaring products later.
- Opening quantity and price are non-negative. Quantity precision follows the
  existing product minimum-order precision (`decimal(18,3)`); money uses
  `decimal(18,2)`.
- `AvailableQuantity = CurrentQuantity - ReservedQuantity` is derived state.
  Opening creates `OpeningQuantity = CurrentQuantity`, `ReservedQuantity = 0`,
  and one immutable `OPENING` ledger entry per listing.
- Quantity can change only through a Domain operation that validates the
  result, increments the listing version, and creates the corresponding ledger
  entry. The new current quantity cannot be negative or less than reserved
  quantity.
- Every price change creates a `PRICE_CHANGE` ledger entry. Every manual stock
  change creates an `ADJUSTMENT` entry containing before/after quantity,
  signed delta, actor, timestamp, and reason.
- Hiding a listing removes it from buyer-visible availability but preserves its
  quantities and history. `SOLD_OUT` is derived when available quantity is
  zero; a client cannot set it directly.
- `expectedVersion` is required for listing mutations. EF Core treats
  `Version` as a concurrency token; stale writes return `409 Conflict`. The
  listing mutation and its ledger entry commit in one database transaction.
- Closing changes only an open session to `CLOSED`; a second close or mutation
  after close returns `409 Conflict`. Reconciliation and active reservation
  checks are deferred until their owning workflows exist.
- Ledger rows are append-only. The API provides no update or delete route for
  them.

## Response and error contract

Session responses include session identity, stall, business date, status,
open/close audit fields, notes, and listing summaries. Listing responses expose
the product-stall and snapshot fields, public price, all four quantity values,
status, and version. Ledger responses expose transaction type, quantity and
price before/after values, reference fields, reason, actor, and occurrence
time. Domain entities are not returned directly.

Successful responses use `ApiResponse<T>` and paged results use the existing
`PagedResult<T>`. Failures use the established Problem Details pipeline:

- `400 Bad Request`: invalid IDs, quantities, prices, reason, filter, or page.
- `401 Unauthorized`: missing or invalid authentication.
- `403 Forbidden`: authenticated vendor does not own the stall.
- `404 Not Found`: stall, current/previous session, product-stall, or listing
  is absent within the addressed stall.
- `409 Conflict`: duplicate session/listing, closed session mutation,
  insufficient stock, invalid state transition, or stale version.

## TDD implementation sequence

Implement this as small red-green-refactor increments; do not build all
production layers before the first tests fail.

1. **Harden the Inventory Domain model.** Add focused tests named in the
   `Method_Scenario_ExpectedResult` format for opening quantities, derived
   availability/status, adjustments, price changes, version increments,
   closed-session mutations, and ledger facts. Replace unrestricted mutation
   of protected inventory state with intention-revealing Domain methods.
2. **Open and read a daily session.** Add Application commands/queries, DTOs,
   validation, ownership/reference ports, handlers, and typed Inventory
   exceptions. Use `TimeProvider` plus a business-clock abstraction so date
   boundary behavior is deterministic in tests. Opening the session and its
   initial listings is one transaction.
3. **Persist the Inventory aggregate.** Add Inventory `DbSet`s and EF Core
   configurations for sessions, listings, and ledgers under an `inventory`
   schema. Configure foreign keys, enum strings, decimal precision, check
   constraints, uniqueness, concurrency, and restrictive deletes. Add and
   inspect a migration; do not hand-edit generated migration artifacts.
   Explicitly prevent the reservation navigation from convention-discovering
   the still-unimplemented Sales and Reservation persistence graphs.
4. **Add and patch daily listings.** Test ownership, active product-stall
   membership, duplicate prevention, default-price snapshotting, explicit
   zero prices, visibility changes, price ledger creation, stale versions, and
   closed sessions before implementing the handlers and repository methods.
5. **Adjust stock atomically.** Test positive and negative deltas, zero-delta
   rejection, insufficient stock, mandatory reasons, exact before/after
   values, actor/time attribution, rollback, and competing updates. Persist
   the listing and ledger entry in the same unit of work.
6. **Read history and close the session.** Add Dapper projections for current,
   previous, and paged ledger reads. Test stall scoping, filters, paging,
   stable ordering, not-found behavior, and the open-to-closed transition.
7. **Expose the Minimal APIs.** Add route constants, request records, endpoint
   mappings, `VendorOnly` authorization, claim-to-actor mapping, `ApiResponse`
   wrappers, OpenAPI response metadata, Inventory exception translation, DI
   registration, and `Program.cs` composition.
8. **Exercise real boundaries.** Add PostgreSQL integration tests for unique
   constraints, precision/check constraints, optimistic concurrency, atomic
   ledger writes, Dapper projections, vendor ownership, authentication, HTTP
   status/body contracts, and Swagger discovery.
9. **Synchronize documentation.** After the behavior exists, update
   `ARCHITECTURE.md` and populate `docs/agent-guides/inventory.md` with only
   verified paths, invariants, transaction boundaries, and focused commands.

## Acceptance criteria

- A vendor can open today's session independently for each owned active stall,
  seed it from that stall's active products, and retrieve it.
- Opening the same stall and business date twice cannot create duplicates,
  including under concurrent requests.
- A vendor can add a daily listing, change its price/visibility, adjust its
  stock, review the audit ledger, and close the session; another vendor cannot.
- Every quantity or price mutation is traceable and commits atomically with its
  ledger entry.
- Negative or over-reserved inventory and lost concurrent updates are blocked
  at both Domain and persistence boundaries.
- The resulting listing is the single inventory source that the later POS and
  online reservation slices can consume without changing this API contract.
