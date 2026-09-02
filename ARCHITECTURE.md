# Haggly Architecture

This document describes the architecture that is currently present in the
repository. Planned modules and technologies are called out separately; they
must not be read as implemented functionality.

## Current direction

Haggly is a .NET 10 modular-monolith scaffold for a digital Vietnamese market.
The MVP domain is divided into business modules, while the executable
application is currently delivered as one API and one relational database.
The completed vertical slices are Identity, Markets, Catalog, Inventory, and
the current executable portions of Sales. Identity provides
registration, login, JWT authentication, authorization policies, vendor
administration, and the current-user endpoint. Markets provides market and
stall CRUD use cases, PostgreSQL persistence, and API endpoints. Catalog
provides Category and Product creation and authenticated reads, PostgreSQL
persistence, and API endpoints. Inventory provides one continuous inventory per
stall, stock items, adjustments, ledger reads, optimistic concurrency, PostgreSQL
persistence, and vendor API endpoints.

The MVP requirements also cover ProductStall, Negotiation, Sales, Payments,
and Finance. Sales currently implements buyer carts, cart checkout into
multi-stall negotiating orders, buyer order reads/cancellation, and vendor POS;
POS revenue and simulated online payment completion are also implemented.
Negotiation, reservation expiration, pickup/fulfillment transitions, real
payment provider integration, and broader Finance reporting remain future workflows.

## Technology actually used

- .NET SDK `10.0.201`, target framework `net10.0`, and C# `14.0`.
- ASP.NET Core Web SDK with Minimal API endpoint mapping.
- Entity Framework Core `10.0.10` with the Npgsql PostgreSQL provider `10.0.3`.
- Dapper `2.1.79` for read-side query adapters backed by PostgreSQL.
- MassTransit `8.5.10` with the RabbitMQ transport for asynchronous integration
  events. The API process currently hosts the bus and consumers.
- PostgreSQL transactional outbox and Inbox persistence, Dapper-backed message
  adapters, a configurable hosted outbox publisher, and structured consumer-fault
  logging.
- ASP.NET Core JWT bearer authentication and `Microsoft.Extensions.Identity.Core`
  password hashing.
- Swashbuckle.AspNetCore `10.2.3` for the OpenAPI/Swagger document and UI.
- xUnit `2.9.3` with the Microsoft .NET test SDK.
- PostgreSQL 17 for local development through `docker-compose.yml`.

Runtime timestamps are represented as `DateTimeOffset`, created from UTC
clocks (`TimeProvider.GetUtcNow()`/`DateTimeOffset.UtcNow`), normalized to
offset `+00:00` at application boundaries, and persisted as PostgreSQL
`timestamp with time zone`. Local time zones are used only when deriving a
business calendar date for display or daily business rules.

The repository uses xUnit for tests and NSubstitute only for Application-port
substitutes in the active unit suite. It does not currently reference
FluentValidation, FluentAssertions, Testcontainers, or OpenTelemetry.

## Current solution structure

The target solution structure contains four production projects and one active
test project; the functional-test project is added in the next testing phase:

```text
Haggly/
|-- Haggly.slnx
|-- src/
|   |-- Haggly.Domain/
|   |-- Haggly.Application/
|   |-- Haggly.Infrastructure/
|   `-- Haggly.Api/
|-- tests/
|   `-- Haggly.UnitTests/
|-- docs/
|-- database/
|-- deploy/
|-- Directory.Build.props
|-- Directory.Packages.props
|-- global.json
`-- README.md
```

There is a `tests/Haggly.ArchitectureTests` directory containing build output,
but no project file and no solution entry for it. It is therefore not an
active test project.

## Dependency boundaries

The current project references establish these boundaries:

```text
Haggly.Domain          -> no Haggly project
Haggly.Application     -> Haggly.Domain
Haggly.Infrastructure  -> Haggly.Application, Haggly.Domain
Haggly.Api             -> Haggly.Application, Haggly.Infrastructure, Haggly.Domain
Haggly.UnitTests              -> Domain, Application
```

The intended rule is that business behavior remains independent of transport
and storage:

- Domain owns business state and rules and must not reference ASP.NET Core, EF
  Core, Dapper, or provider APIs.
- Application owns use-case orchestration, validation, application contracts,
  and ports for external dependencies.
- Infrastructure implements persistence, authentication, hashing, and other
  provider-facing adapters.
- API owns HTTP routes, request/response mapping, authorization registration,
  middleware, and OpenAPI configuration.

The API currently references Domain directly because endpoint authorization and
claim handling use Identity role/domain types. New business behavior should be
placed in Application and exposed through an Application contract rather than
implemented in an endpoint.

## Domain modules

All current business model files are in
`src/Haggly.Domain/Modules`. The module folders and their observed model types
are:

| Module | Current Domain types | Current implementation state |
|---|---|---|
| Identity | `User`, `Role`, `UserRole`, `BuyerProfile`, `VendorProfile`, `AdminProfile`, `DelivererProfile`, related enums | Implemented vertical slice across all layers |
| Markets | `Market`, `Stall`, related enums | Implemented vertical slice across Domain, Application, Infrastructure, and API |
| Catalog | `Category`, `Product`, `ProductStall`, related enums | Category, Product, and ProductStall vertical slices implemented |
| Inventory | `Inventory`, `InventoryItem`, `InventoryLedger`, related enums | Implemented continuous-inventory slice plus aggregate payment-time stock holds across Domain, Application, Infrastructure, and API |
| Negotiation | `NegotiationSession`, `NegotiationOffer`, `NegotiationOfferItem`, `NegotiationMessage`, related enums | Domain model scaffold only |
| Sales | `Cart`, `CartItem`, `Order`, `OrderItem`, `StallFulfillment`, `PosSale`, `PosSaleItem`, related enums | Buyer cart/checkout and order create/read/cancel implemented; POS completion and history implemented; later order lifecycle remains incomplete |
| Payments | `Payment`, `PaymentAllocation`, `PaymentMethod`, `PaymentTransaction`, related enums | Simulated processing atomically persists the Payment, attempt, per-stall allocations, and result event containing allocation IDs |
| Finance | `RevenueLedger`, `RevenueEntryType` | Append-only POS and online-payment revenue implemented; a dedicated MassTransit PaymentSucceeded consumer invokes the Finance handler and appends one row per allocation |

Negotiation is currently a top-level Domain module under
`Modules/Negotiation`; it is not nested under `Modules/Sales`.

The shared Domain layer contains the base types `Entity`, `ImmutableEntity`,
`AuditableEntity`, `AuditableRecord`, and `SoftDeletableEntity` in
`Haggly.Domain/Common`.

## Implemented vertical slices

### Application

Identity Application code is organized by use case:

```text
Haggly.Application/
|-- Abstractions/Identity/
|   |-- registration and login repository contracts
|   |-- registration and login use-case contracts
|   |-- password hasher contract
|   `-- identity token-service contract
`-- Modules/Identity/
    |-- Registration/
    |   |-- Commands/
    |   |   |-- RegisterBuyerCommand and RegisterBuyerHandler
    |   |   `-- RegisterVendorCommand and RegisterVendorHandler
    |   |-- DTOs, validation, and exceptions
    |   `-- ...
    `-- Login/
        |-- Commands/LoginCommand and LoginHandler
        |-- DTOs
        |-- validation
        `-- authentication/validation exceptions
```

Validation is implemented with local validation classes and application
exceptions. FluentValidation is not currently used.

Markets Application code is organized by aggregate and operation:

```text
Haggly.Application/
|-- Abstractions/Markets/
|   |-- market and stall command repository contracts
|   `-- market and stall query contracts
`-- Modules/Markets/
    |-- Commands/Markets and Commands/Stalls (requests and handlers)
    |-- Queries/Markets and Queries/Stalls (requests and handlers)
    |-- DTOs/Markets and DTOs/Stalls
    |-- Validation/Markets and Validation/Stalls
    `-- module-specific exceptions
```

Catalog Category and Product Application code follows the same command/query
and handler co-location convention, with a separate port split. Category includes `CreateCategory`, `GetCategories`, and
`GetCategoryById`; Product includes `CreateProduct`, `GetProducts`, and
`GetProductById`; ProductStall includes create, paginated read, read-by-id, and
patch use cases. These have DTOs, handlers, validation, exceptions, and
separate command-repository and query ports. New catalog definitions are active
by default; Category slugs are normalized to lowercase and unique among
non-deleted categories, while Product names are unique within a category among
non-deleted products.

### Infrastructure

`Haggly.Infrastructure` contains:

- `Persistence/HagglyDbContext`, EF Core configurations, Identity, Markets,
  Catalog, Inventory, Sales, Payments, and Finance repositories, the design-time context
  factory, and their migrations.
- `Persistence/DapperDbContext` and Dapper query adapters for Identity,
  Markets, Catalog, Inventory, Cart, Order, and POS Sales reads. EF Core remains
  the transactional write adapter for cart/order changes, cart checkout, POS
  completion, and POS inventory/revenue ledger updates.
- `Authentication/JwtTokenService`, JWT options/configuration, and
  `AspNetPasswordHasher`.
- `Messaging/RabbitMqOptions`, MassTransit RabbitMQ registration, and the
  broker-facing implementation of the Application domain-event publisher port,
  the Dapper outbox writer/processor, Inbox repository, hosted outbox publisher,
  payment request/result consumers, and centralized payment-fault logging
  consumer. Each business reaction uses an independent durable queue.

`AddPersistence` requires the `ConnectionStrings:HagglyDatabase` setting and
configures PostgreSQL. The current `HagglyDbContext` exposes DbSets and EF Core
mappings for Identity, Markets, Catalog, continuous Inventory, Cart, Order,
POS Sales, Payments, and POS Finance revenue. The migrations include the
continuous-inventory refactor, POS/revenue persistence, Sales orders, and buyer
carts.
Product and ProductStall are mapped to `catalog.products` and
`catalog.product_stalls`; Inventory is mapped to `inventory`, POS sales to
`sales`, Cart and Order are mapped to `sales`, and POS revenue to `finance`.
Reservations have no separate entity or table; `InventoryItem.ReservedQuantity`
stores the aggregate payment-time hold and active OrderItems provide the quantities.
Negotiation remains unmapped. Payments maps
`payments.payments`, `payments.payment_transactions`, and
`payments.payment_allocations`. Revenue rows use a unique PaymentAllocation plus
entry-type key for idempotency. `messaging.inbox_messages` deduplicates the
Inventory and Order `PaymentFailedEvent` consumers; adoption by the other
consumers remains deferred. Finance, Inventory, and Sales/Order implement
PaymentSucceeded Application handlers, persistence adapters, dedicated queues,
and MassTransit consumers.

### API

`src/Haggly.Api/Program.cs` composes the application by registering persistence,
token services, and API services. The request pipeline uses exception handling,
authentication, and authorization middleware.

Payments exposes buyer-authorized `POST /api/v1/payments`. It validates Order
ownership and eligibility, atomically reserves the Order's active item
quantities, moves the Order to `PAYMENT_PENDING`, creates a pending Payment plus
`PaymentRequested`, and returns `202 Accepted`.

Identity routes are grouped under `/api/v1/identity`:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/register/buyer` | Register a buyer |
| `POST` | `/register/vendor` | Register a vendor |
| `POST` | `/login` | Issue a JWT access token |
| `GET` | `/me` | Read the authenticated user and roles |

The API exposes `BuyerOnly`, `VendorOnly`, `AdminOnly`, and
`CatalogContributor` policies. `AdminOnly` accepts `MARKET_ADMIN` or
`PLATFORM_ADMIN`; `CatalogContributor` additionally accepts `VENDOR`. Resource
ownership checks remain the responsibility of the owning business use case.

Markets routes are grouped under `/api/v1/markets` and expose market and stall
CRUD operations. Market and stall write operations require the configured admin
authorization policy; reads use Application query contracts backed by Dapper
adapters.

Category routes are grouped under `/api/v1/categories`:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/api/v1/categories` | Create a category; requires `CatalogContributor` |
| `GET` | `/api/v1/categories` | List active categories; requires authentication |
| `GET` | `/api/v1/categories/{id}` | Read one active category; requires authentication |

Product routes are grouped under `/api/v1/products`:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/api/v1/products` | Create a product; requires `CatalogContributor` |
| `GET` | `/api/v1/products` | List active products; accepts optional `categoryId`; requires authentication |
| `GET` | `/api/v1/products/{id}` | Read one active product; requires authentication |

Stall product routes are grouped under `/api/v1/stalls/{stallId}/products`:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/api/v1/stalls/{stallId}/products` | Attach a catalog product; requires the stall owner |
| `GET` | `/api/v1/stalls/{stallId}/products` | Paginated stall-product list; requires authentication |
| `GET` | `/api/v1/stalls/{stallId}/products/{id}` | Read one stall product; requires authentication |
| `PATCH` | `/api/v1/stalls/{stallId}/products/{id}` | Update stall configuration; requires the stall owner |

Inventory routes are grouped under `/api/v1/vendor/stalls/{stallId}` and
require the `VendorOnly` policy:

| Method | Route | Behavior |
|---|---|---|
| `GET` | `/api/v1/vendor/stalls/{stallId}/inventory` | Read the continuous inventory and its items |
| `POST` | `/api/v1/vendor/stalls/{stallId}/inventory/items` | Add a configured stall product with current quantity |
| `GET` | `/api/v1/vendor/stalls/{stallId}/inventory/items/{inventoryItemId}` | Read one inventory item |
| `POST` | `/api/v1/vendor/stalls/{stallId}/inventory/adjustments` | Apply a signed stock adjustment with `expectedVersion` |
| `GET` | `/api/v1/vendor/stalls/{stallId}/inventory/ledger` | Filter and page ledger entries |

Vendor POS routes are grouped under `/api/v1/vendor/stalls/{stallId}/pos-sales`
and require the `VendorOnly` policy:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/api/v1/vendor/stalls/{stallId}/pos-sales` | Complete an idempotent sale and deduct inventory atomically |
| `GET` | `/api/v1/vendor/stalls/{stallId}/pos-sales` | Page the stall's completed POS history |
| `GET` | `/api/v1/vendor/stalls/{stallId}/pos-sales/{posSaleId}` | Return one POS sale with its item details |

Buyer cart routes are grouped under `/api/v1/cart` and require the `BuyerOnly`
policy:

| Method | Route | Behavior |
|---|---|---|
| `GET` | `/api/v1/cart` | Return the current buyer cart enriched with live stall, product, offering, and remaining Inventory data |
| `POST` | `/api/v1/cart/items` | Add an InventoryItem, creating the buyer cart when necessary |
| `PUT` | `/api/v1/cart/items/{cartItemId}` | Replace the cart item's requested quantity and notes |
| `DELETE` | `/api/v1/cart/items/{cartItemId}` | Remove one cart item |
| `DELETE` | `/api/v1/cart` | Clear the buyer cart |
| `POST` | `/api/v1/cart/checkout` | Revalidate all lines, create a negotiating multi-stall Order, and clear the cart transactionally |

Buyer order routes are grouped under `/api/v1/orders` and require the
`BuyerOnly` policy:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/api/v1/orders` | Create a negotiating multi-stall Order directly from submitted InventoryItem lines |
| `GET` | `/api/v1/orders` | Page the authenticated buyer's orders |
| `GET` | `/api/v1/orders/{orderId}` | Return an owned Order with fulfillments and item snapshots |
| `POST` | `/api/v1/orders/{orderId}/cancel` | Cancel an eligible owned Order |

Cart availability uses `CurrentQuantity - ReservedQuantity`. Cart lines and
checkout do not reserve or deduct Inventory. Starting payment revalidates and
reserves every active OrderItem atomically with Payment creation and its outbox
message. Checkout creates the Order and clears the cart in one EF Core transaction.

Successful endpoint responses use `ApiResponse<T>`. Application exceptions and
authentication failures are translated centrally to Problem Details. In
Development, Swagger UI is available at `/swagger`, and `/` redirects to it.

JWT settings are read from the `Jwt` configuration section. Issuer, audience,
signing key length, lifetime, signature, and token lifetime are validated; the
current bearer configuration accepts HMAC-SHA256 tokens with zero clock skew.

## Persistence and runtime topology

The runtime is a single ASP.NET Core process backed by PostgreSQL and a
configured RabbitMQ connection:

```text
HTTP client
    |
    v
Haggly.Api
    |-- Haggly.Application use cases
    |-- Haggly.Infrastructure authentication and persistence
    |-- MassTransit publisher bus
    `-- Haggly.Domain model
    |                         |
    v                         v
PostgreSQL                RabbitMQ
(local: localhost:5433)   (local: localhost:5672)
```

The implemented payment message flow is:

```text
POST /api/v1/payments
  -> PostgreSQL transaction: reserve Inventory + update Order + create Payment
     + append PaymentRequested to messaging.outbox_messages
  -> hosted outbox publisher -> payments.payment-requested.v1
  -> payments-payment-requested-v1 -> simulated provider adapter
  -> PostgreSQL transaction: persist result + append one result event
     |-> payments.payment-succeeded.v1
     |    |-> finance-payment-succeeded-v1
     |    |-> inventory-payment-succeeded-v1
     |    `-> order-payment-succeeded-v1
     `-> payments.payment-failed.v1
          |-> inventory-payment-failed-v1
          `-> order-payment-failed-v1

Terminal result-consumer faults
  -> MassTransit Fault<PaymentSucceededEvent> or Fault<PaymentFailedEvent>
  -> payment-processing-faults-v1 -> structured ILogger error
```

These module reactions are eventually consistent and use at-least-once broker
delivery. The outbox makes each originating database change atomic with its
message creation; it does not make all downstream module changes one distributed
transaction. Consumer-specific idempotency or Inbox claims protect implemented
reactions from duplicate delivery.

The local PostgreSQL database and RabbitMQ broker are defined in
`docker-compose.yml`. The development configuration uses the
`HagglyDatabase` connection-string name and the `RabbitMq` configuration
section. Database business behavior must remain owned by the relevant module;
EF Core mappings and repositories are adapters, not business-rule owners.

## Testing structure

`Haggly.UnitTests` is the active business-test project. Its `Domain` tree uses
real entities and aggregates without substitutes. Its `Application` tree is
organized by module and use case; tests construct real handlers and Domain
objects while NSubstitute replaces only Application ports. Tests use explicit
Arrange/Act/Assert sections, deterministic data, and
`Method_Scenario_ExpectedResult` names. The project references Domain and
Application only, so Infrastructure and API changes cannot leak into business
unit tests.

The repository currently has no active functional-test or architecture-test
project. The next test phase will add `Haggly.FunctionalTests` for real HTTP,
PostgreSQL, authentication, transaction, messaging, and provider boundaries.

## Module growth rules

When a new module becomes executable, complete one vertical slice through the
existing boundaries:

1. Put invariants and state transitions in the module's Domain types.
2. Add an Application use case, validation, authorization requirements, and
   explicit ports where needed.
3. Add Infrastructure mappings/adapters and a migration when persistence is
   required.
4. Add API endpoints only for transport concerns and map application failures
   to the established Problem Details contract.
5. Add focused tests to `Haggly.UnitTests`; add real boundary coverage only when
   the change crosses persistence, authentication, messaging, HTTP, or a provider.

Cross-module workflows should have one coordinating Application use case.
Modules should communicate through explicit contracts and must not directly
mutate another module's entities.

## Proposed future state

The following is direction, not a description of files that currently exist:

- Complete reservation expiration, Negotiation, payment retries, fulfillment/pickup, and
  broader Payments/Finance workflows around the existing Cart and Order slices.
- Extend EF Core configuration and migrations as each module gains a persisted
  use case.
- Add architecture tests only when an actual architecture-test project and its
  solution entry are created.
- Add `Haggly.FunctionalTests` incrementally, starting from named critical
  journeys rather than copying unit-test structure.
- Add durable fault/incident storage, replay or reconciliation, and Loki/Grafana
  only when the payment workflow has an explicit operational recovery target.

The README remains the source for MVP scope and business requirements. This
document records executable structure and boundaries; it does not expand the
MVP into delivery, multi-market, or other excluded functionality.
