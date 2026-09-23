# Haggly API Plan

This document defines the next API slices for the Haggly MVP. Existing behavior
is marked as implemented; the remaining APIs are planned in dependency order.

## Current implemented APIs

### Identity

```http
POST /api/v1/identity/register/buyer
POST /api/v1/identity/register/vendor
POST /api/v1/identity/login
GET  /api/v1/identity/me
```

### Vendor administration

```http
GET  /api/v1/admin/vendors
POST /api/v1/admin/vendors/{vendorId}/approve
POST /api/v1/admin/vendors/{vendorId}/reject
POST /api/v1/admin/vendors/{vendorId}/suspend
```

The vendor list supports `approvalStatus`, `search`, `page`, and `pageSize`.
Vendor actions require the `AdminOnly` policy and return the updated vendor.

### Market and stall administration

```http
GET    /api/v1/markets
GET    /api/v1/markets/{id}
POST   /api/v1/markets
PUT    /api/v1/markets/{id}
DELETE /api/v1/markets/{id}

GET    /api/v1/markets/stalls
GET    /api/v1/markets/stalls/{id}
POST   /api/v1/markets/stalls
PUT    /api/v1/markets/stalls/{id}
DELETE /api/v1/markets/stalls/{id}
```

These routes are currently administrative CRUD endpoints with soft deletes.

### Catalog and stall-product APIs

```http
POST /api/v1/categories
GET  /api/v1/categories
GET  /api/v1/categories/{id}

POST /api/v1/products
GET  /api/v1/products
GET  /api/v1/products/{id}

POST  /api/v1/stalls/{stallId}/products
GET   /api/v1/stalls/{stallId}/products
GET   /api/v1/stalls/{stallId}/products/{id}
PATCH /api/v1/stalls/{stallId}/products/{id}
```

Category and product reads use Dapper query adapters. Product-stall writes are
owner-authorized and preserve catalog identity separately from stall-specific
pricing and availability.

### Continuous inventory APIs

```http
GET  /api/v1/vendor/stalls/{stallId}/inventory
POST /api/v1/vendor/stalls/{stallId}/inventory/items
GET  /api/v1/vendor/stalls/{stallId}/inventory/items/{inventoryItemId}
POST /api/v1/vendor/stalls/{stallId}/inventory/adjustments
GET  /api/v1/vendor/stalls/{stallId}/inventory/ledger
```

Inventory quantity mutations are synchronous and transactional. They update
the current inventory item and append an immutable inventory ledger entry.

### Vendor POS APIs

```http
POST /api/v1/vendor/stalls/{stallId}/pos-sales
GET  /api/v1/vendor/stalls/{stallId}/pos-sales
GET  /api/v1/vendor/stalls/{stallId}/pos-sales/{posSaleId}
```

POS completion is idempotent and atomically deducts inventory, creates the POS
sale, records the inventory ledger entry, and records the revenue ledger entry.
POS history uses a Dapper header-only query; the detail endpoint loads item
lines by `posSaleId`. Both endpoints require the owning vendor.

## Remaining implementation order

1. Identity profiles and vendor-owned stall APIs.
2. Public market and stall discovery.
3. Buyer cart APIs (order APIs implemented; cart deferred).
4. Negotiation.
5. Payment and pickup.
6. Broader revenue and reporting APIs (summary revenue MVP implemented).

Each slice should be implemented vertically through Domain, Application,
Infrastructure, API, and focused tests. Cross-module workflows belong to one
coordinating Application use case.

## 1. Identity profiles and vendor stall ownership

### Profile APIs

```http
GET   /api/v1/identity/me/profile
PATCH /api/v1/identity/me/profile
POST  /api/v1/identity/me/change-password
```

Rules:

- The authenticated subject identifies the user; user IDs are not accepted from
  the request body.
- Users may update their own contact and profile fields only.
- Roles, approval status, account status, password hash, and audit fields are
  server-controlled.
- Email and phone changes should be validated for uniqueness and may later
  require verification.

### Vendor-owned stall APIs

```http
POST /api/v1/vendor/stalls
GET  /api/v1/vendor/stalls
GET  /api/v1/vendor/stalls/{stallId}
PUT  /api/v1/vendor/stalls/{stallId}
```

Rules:

- `VendorId` is derived from the JWT and never trusted from the request body.
- Vendors can only read or modify their own stalls.
- A new stall starts in `PENDING` status.
- A vendor must be approved and active before a stall can become active.

### Stall lifecycle APIs

```http
POST /api/v1/admin/stalls/{stallId}/approve
POST /api/v1/admin/stalls/{stallId}/suspend
POST /api/v1/admin/stalls/{stallId}/close
```

Rules:

- The market must be active for a stall to be approved.
- The owning vendor must have `ApprovalStatus = APPROVED` and
  `UserStatus = ACTIVE`.
- Invalid status transitions return `409 Conflict`.
- Stall ownership changes are deferred until a dedicated transfer use case is
  needed.

## 2. Public market discovery

```http
GET /api/v1/public/markets
GET /api/v1/public/markets/{marketId}/stalls
GET /api/v1/public/stalls/{stallId}
```

Optional query parameters:

```text
search, page, pageSize
```

Public results include only non-deleted active markets and active stalls owned
by approved, active vendors. Administrative status and audit fields should not
be exposed in public projections unless required by the client.

## 3. Catalog and product APIs (implemented)

### Vendor product management

```http
GET  /api/v1/vendor/products
POST /api/v1/vendor/products
PUT  /api/v1/vendor/products/{productId}
POST /api/v1/vendor/products/{productId}/deactivate
```

### Buyer catalog browsing

```http
GET /api/v1/categories
GET /api/v1/stalls/{stallId}/products
GET /api/v1/products?search=&categoryId=&stallId=&page=&pageSize=
```

Rules:

- Product identity and category are Catalog-owned.
- Availability and quantity are Inventory-owned and must not be stored as
  mutable product identity fields.
- Vendors can manage only products associated with their own stalls.
- Deactivated products remain in history but are excluded from active browsing.

## 4. Continuous inventory APIs (implemented)

```http
GET  /api/v1/vendor/stalls/{stallId}/inventory
POST /api/v1/vendor/stalls/{stallId}/inventory/items
GET  /api/v1/vendor/stalls/{stallId}/inventory/items/{inventoryItemId}
POST /api/v1/vendor/stalls/{stallId}/inventory/adjustments
GET  /api/v1/vendor/stalls/{stallId}/inventory/ledger
```

Rules:

- Current quantity, reserved quantity, and available quantity are distinct
  values.
- Current quantity cannot become negative.
- Every adjustment records actor, timestamp, reason, and before/after values.
- Online orders and offline POS sales use the same inventory source.

## 5. Vendor POS APIs (implemented)

```http
POST /api/v1/vendor/stalls/{stallId}/pos-sales
GET  /api/v1/vendor/stalls/{stallId}/pos-sales
GET  /api/v1/vendor/stalls/{stallId}/pos-sales/{posSaleId}
```

Rules:

- The vendor may sell only inventory belonging to their stall.
- A POS sale immediately decreases current and available inventory.
- The sale creates inventory and revenue ledger entries in the same transaction.
- Repeating the same client request ID returns the existing sale without
  applying inventory changes twice.
- The sale cannot exceed available quantity.

## 6. Buyer cart and multi-stall order APIs

The buyer order slice is implemented. Cart APIs remain intentionally deferred.

Deferred cart routes:

```http
GET    /api/v1/cart
POST   /api/v1/cart/items
PUT    /api/v1/cart/items/{itemId}
DELETE /api/v1/cart/items/{itemId}
```

Implemented order routes:

```http
POST /api/v1/orders
GET  /api/v1/orders
GET  /api/v1/orders/{orderId}
POST /api/v1/orders/{orderId}/cancel
```

Rules:

- A buyer owns and can modify only their own cart and orders.
- One order may contain products from multiple stalls.
- The buyer sees one order; the system creates one stall fulfillment per stall.
- Inventory is reserved only after the relevant vendor agreement/order step.
- Vendors see only their own stall fulfillment.

## 7. Negotiation APIs

```http
POST /api/v1/orders/{orderId}/stalls/{stallId}/negotiations
GET  /api/v1/negotiations/{negotiationId}
POST /api/v1/negotiations/{negotiationId}/offers
POST /api/v1/negotiations/{negotiationId}/accept
POST /api/v1/negotiations/{negotiationId}/reject
GET  /api/v1/negotiations/{negotiationId}/messages
POST /api/v1/negotiations/{negotiationId}/messages
```

Rules:

- Negotiation is isolated per stall fulfillment.
- Only negotiable products may participate.
- Offers can change quantity, unit price, or both.
- Chat messages never mutate order totals.
- Order values change only when an offer is explicitly accepted.

## 8. Payment and pickup APIs

### Payment

```http
POST /api/v1/orders/{orderId}/payments
GET  /api/v1/orders/{orderId}/payments
POST /api/v1/payments/{paymentId}/retry
POST /api/v1/payments/{paymentId}/refund
```

Supported methods for the MVP are cash, bank transfer, and manual/simulated QR.
Payment records must preserve transaction history and match the confirmed order
amount.

### Fulfillment and pickup

```http
POST /api/v1/stall-fulfillments/{fulfillmentId}/prepare
POST /api/v1/stall-fulfillments/{fulfillmentId}/ready
POST /api/v1/stall-fulfillments/{fulfillmentId}/pickup
GET  /api/v1/orders/{orderId}/pickup-status
```

Rules:

- Only the owning vendor can mark a fulfillment prepared or ready.
- Pickup is confirmed by the vendor.
- A fulfillment cannot be picked up twice.
- An order is completed only after all active stall fulfillments are picked up.

## 9. Revenue and reporting APIs (summary MVP implemented)

```http
GET /api/v1/vendor/reports/revenue?from=&to=&saleChannel=&stallId=
GET /api/v1/admin/reports/revenue?from=&to=&saleChannel=&marketId=&vendorId=&stallId=
```

Rules:

- Vendors see only their own current stall data; an inaccessible selected stall
  is returned as not found.
- Reports count one completed POS transaction or one paid online stall
  fulfillment as one sale. A multi-stall online order therefore contributes one
  sale to each participating stall.
- `NetRevenue` is the sum of matching `SALE` revenue-entry `NetAmount` values.
- `saleChannel` accepts `ALL`, `POS`, or `ONLINE` and defaults to `ALL`.
- `from` defaults to 00:00 UTC today and `to` defaults to the current UTC time.
  Supplied values are normalized to UTC and the range cannot exceed 366 days.
- Vendor responses contain totals and per-stall summaries. Administrator
  responses contain overall totals grouped by vendor and stall.
- Reports are projections over revenue entries and do not become a second source
  of truth.

Deferred reporting work:

- Refund processing and refund-aware reporting.
- Detailed sales and order rows.
- Inventory reporting.
- Administrative audit logs preserving actor and timestamp information.
- CSV or spreadsheet exports and market-specific administrator scope.

## Common API conventions

- Use `/api/v1` for all routes.
- Successful responses use `ApiResponse<T>`.
- Validation, not-found, conflict, authentication, and authorization failures
  use the existing Problem Details pipeline.
- Commands should derive ownership and actor identity from authenticated claims.
- List endpoints should support stable ordering, bounded page sizes, and DTO
  projections rather than exposing Domain entities.
- New behavior requires focused unit tests and integration tests at real
  persistence/authentication boundaries.

## Explicitly out of scope for this MVP

- Home delivery and driver APIs.
- Route optimization or live delivery tracking.
- Multiple-market tenancy beyond the current market model.
- Loyalty programs, promotions, and AI demand forecasting.
