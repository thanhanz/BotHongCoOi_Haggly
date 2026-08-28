# Sales module guide

This guide records the currently executable Cart, buyer Order, vendor POS, and
payment-result Order reactions. Negotiation, reservation expiration,
preparation, and pickup workflows remain incomplete.

## Responsibilities

Sales owns buyer carts, cart items, customer orders, per-stall fulfillments,
order item snapshots, and completed vendor POS sales. Catalog owns product and
ProductStall identity, price, selling unit, minimum order quantity, activation,
and negotiability. Inventory owns current and reserved quantities and all stock
mutations.

Cart and order workflows read Inventory and Catalog through explicit
Application ports. They do not mutate Inventory entities directly.

## Layer map

- Domain: `src/Haggly.Domain/Modules/Sales` contains `Cart`, `CartItem`,
  `Order`, `OrderItem`, `StallFulfillment`, `PosSale`, and `PosSaleItem`.
- Application: `src/Haggly.Application/Modules/Sales` contains cart, order, and
  POS commands, queries, DTOs, validation, and exception contracts.
- Infrastructure: EF repositories/configurations and Dapper read adapters are
  under `Persistence/Repositories/Sales`, `Configurations/Sales`, and
  `Queries/Sales`.
- API: `src/Haggly.Api/Endpoints/Sales` exposes buyer cart/order routes and
  vendor POS routes.

## Cart invariants and behavior

- The database permits one cart per buyer and one occurrence of an
  InventoryItem per cart.
- Cart quantity must be positive. Application add, update, and checkout use
  `ICartCatalog` to require an active Market, Stall, Product, and ProductStall.
- Requested quantity must be at least the ProductStall minimum order quantity
  and no greater than `CurrentQuantity - ReservedQuantity`.
- Duplicate item additions are conflicts; updates and removals require an item
  in the authenticated buyer's cart.
- `GET /api/v1/cart` returns an empty `CartDto` when no persisted cart exists.
  The empty response uses `Guid.Empty` as its cart ID.
- Cart reads use current ProductStall price and configuration. They group lines
  by stall and include current product/stall information, remaining Inventory
  quantity, negotiability, and an `IsQuantityAvailable` flag.
- Inactive or soft-deleted Market, Stall, Product, or ProductStall data leaves
  an existing cart line visible with zero remaining quantity so the buyer can
  remove or correct it.
- Cart quantities do not reserve or deduct Inventory. Availability can change
  after an add or read, so checkout validates every line again.

## Checkout and orders

Checkout rejects a missing or empty cart and rejects any missing, inactive, or
over-quantity line. A valid cart is converted into one `Order`, grouped into a
`StallFulfillment` per stall. The order begins in `NEGOTIATING` status and
stores product name, selling unit, public unit price, requested quantity, and
notes as order-item values.

`EfCartCheckoutUnitOfWork` creates the order and clears the cart within one EF
Core database transaction. Checkout does not reserve Inventory. The subsequent
payment-start use case atomically reserves every active OrderItem, moves the
Order from `AGREED` to `PAYMENT_PENDING`, creates the Payment, and writes the
provider request before it can be published.

The existing `POST /api/v1/orders` path can also create a negotiating order
directly from submitted inventory item lines. Buyer order list, detail, and
cancellation routes remain available under `/api/v1/orders`.

`OrderPaymentSucceededHandler` validates the committed Payment allocations,
marks the Order fully `PAID`, and assigns each fulfillment its complete
`PaidAmount`. Exact duplicate delivery is a no-op. Fulfillments remain `AGREED`;
successful collection does not automatically start vendor preparation. The
MassTransit adapter uses the durable `order-payment-succeeded-v1` queue
bound to `payments.payment-succeeded.v1`, with retries after 1, 5, and 15
seconds.

`OrderPaymentFailedHandler` atomically claims `PaymentFailedEvent` in
InboxMessages and moves a `PAYMENT_PENDING` Order back to `AGREED`. Delayed
failure events do not overwrite `PAID` or `CANCELLED` Orders. Its MassTransit
adapter owns durable queue `order-payment-failed-v1`, retries after 1, 5, and 15
seconds, and retains exhausted messages in the default `_error` queue.

## Persistence

`Cart` and `CartItem` map to `sales.carts` and `sales.cart_items`. The
`CreateCarts` migration adds the buyer uniqueness constraint, the per-cart
inventory-item uniqueness constraint, positive quantity check, and restrictive
foreign keys to buyer and InventoryItem. Cart-item deletion cascades only from
its owning cart.

`EfCartCommandRepository` owns cart writes and translates concurrency/unique
database failures to `CartConflictException`. `DapperCartQuery` builds the
buyer-facing live projection, while `DapperCartCatalog` supplies orderability
and quantity facts for Application commands.

## HTTP routes

Buyer cart routes require `BuyerOnly`:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/v1/cart` | Read the current enriched cart |
| `POST` | `/api/v1/cart/items` | Add an inventory item |
| `PUT` | `/api/v1/cart/items/{cartItemId}` | Update quantity and notes |
| `DELETE` | `/api/v1/cart/items/{cartItemId}` | Remove one line |
| `DELETE` | `/api/v1/cart` | Clear the cart |
| `POST` | `/api/v1/cart/checkout` | Create an order and clear the cart atomically |

Buyer order routes require `BuyerOnly`:

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/orders` | Create a negotiating multi-stall order directly |
| `GET` | `/api/v1/orders` | Page the authenticated buyer's orders |
| `GET` | `/api/v1/orders/{orderId}` | Read an owned order with fulfillments and lines |
| `POST` | `/api/v1/orders/{orderId}/cancel` | Cancel an eligible owned order |

Vendor POS routes remain under
`/api/v1/vendor/stalls/{stallId}/pos-sales` and require `VendorOnly`.

## Tests and verification

Application cart coverage is in
`tests/Haggly.UnitTests/Application/Modules/Sales/Handlers/CartApplicationHandlerTests.cs`.
It covers enriched reads, add/update quantity limits, minimum quantity, remove,
clear, successful multi-stall checkout, checkout after stock reduction, and
empty checkout. There are currently no cart-specific API or PostgreSQL
integration tests.

Focused command:

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-build --filter "FullyQualifiedName~CartApplicationHandlerTests"
```
