# Haggly Architecture

This document describes the architecture that is currently present in the
repository. Planned modules and technologies are called out separately; they
must not be read as implemented functionality.

## Current direction

Haggly is a .NET 10 modular-monolith scaffold for a digital Vietnamese market.
The MVP domain is divided into business modules, while the executable
application is currently delivered as one API and one relational database.
The first completed vertical slice is Identity: registration, login, JWT
authentication, authorization policies, and the current-user endpoint.

The MVP requirements also cover markets, catalog, inventory, negotiation, sales,
payments, and finance. Those areas currently have Domain model types, but their
Application use cases, API endpoints, and persistence mappings have not yet been
implemented.

## Technology actually used

- .NET SDK `10.0.201`, target framework `net10.0`, and C# `14.0`.
- ASP.NET Core Web SDK with Minimal API endpoint mapping.
- Entity Framework Core `10.0.10` with the Npgsql PostgreSQL provider `10.0.3`.
- ASP.NET Core JWT bearer authentication and `Microsoft.Extensions.Identity.Core`
  password hashing.
- Swashbuckle.AspNetCore `10.2.3` for the OpenAPI/Swagger document and UI.
- xUnit `2.9.3` with the Microsoft .NET test SDK.
- PostgreSQL 17 for local development through `docker-compose.yml`.

The repository does not currently reference Dapper, FluentValidation,
FluentAssertions, Testcontainers, OpenTelemetry, or a messaging provider. They
remain possible future choices, not current architectural dependencies.

## Current solution structure

The solution contains four production projects and two test projects:

```text
Haggly/
|-- Haggly.slnx
|-- src/
|   |-- Haggly.Domain/
|   |-- Haggly.Application/
|   |-- Haggly.Infrastructure/
|   `-- Haggly.Api/
|-- tests/
|   |-- Haggly.UnitTests/
|   `-- Haggly.IntegrationTests/
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
Haggly.UnitTests       -> Domain, Application, Infrastructure
Haggly.IntegrationTests -> Api
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
| Markets | `Market`, `Stall`, related enums | Domain model scaffold only |
| Catalog | `Category`, `Product`, `ProductStall`, related enums | Domain model scaffold only |
| Inventory | `InventorySession`, `InventoryLedger`, `InventoryReservation`, `DailyProductListing`, related enums | Domain model scaffold only |
| Negotiation | `NegotiationSession`, `NegotiationOffer`, `NegotiationOfferItem`, `NegotiationMessage`, related enums | Domain model scaffold only |
| Sales | `Order`, `OrderItem`, `StallFulfillment`, related enums | Domain model scaffold only |
| Payments | `Payment`, `PaymentAllocation`, `PaymentMethod`, `PaymentTransaction`, related enums | Domain model scaffold only |
| Finance | `RevenueLedger`, `RevenueEntryType` | Domain model scaffold only |

Negotiation is currently a top-level Domain module under
`Modules/Negotiation`; it is not nested under `Modules/Sales`.

The shared Domain layer contains the base types `Entity`, `ImmutableEntity`,
`AuditableEntity`, `AuditableRecord`, and `SoftDeletableEntity` in
`Haggly.Domain/Common`.

## Implemented Identity vertical slice

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
    |   |-- RegisterBuyerHandler
    |   |-- RegisterVendorHandler
    |   |-- commands, DTOs, validation, and exceptions
    |   `-- ...
    `-- Login/
        |-- LoginHandler
        |-- command and DTOs
        |-- validation
        `-- authentication/validation exceptions
```

Validation is implemented with local validation classes and application
exceptions. FluentValidation is not currently used.

### Infrastructure

`Haggly.Infrastructure` contains:

- `Persistence/HagglyDbContext`, EF Core configurations, Identity repositories,
  the design-time context factory, and the initial Identity migration.
- `Authentication/JwtTokenService`, JWT options/configuration, and
  `AspNetPasswordHasher`.

`AddPersistence` requires the `ConnectionStrings:HagglyDatabase` setting and
configures PostgreSQL. The current `HagglyDbContext` exposes DbSets for the
Identity entities, and the current migration is `InitialIdentity`; no mappings
for the other Domain modules are present.

### API

`src/Haggly.Api/Program.cs` composes the application by registering persistence,
token services, and API services. The request pipeline uses exception handling,
authentication, and authorization middleware.

Identity routes are grouped under `/api/v1/identity`:

| Method | Route | Behavior |
|---|---|---|
| `POST` | `/register/buyer` | Register a buyer |
| `POST` | `/register/vendor` | Register a vendor |
| `POST` | `/login` | Issue a JWT access token |
| `GET` | `/me` | Read the authenticated user and roles |

The API exposes `BuyerOnly`, `VendorOnly`, and `AdminOnly` policies. `AdminOnly`
accepts `MARKET_ADMIN` or `PLATFORM_ADMIN`. Resource ownership checks remain the
responsibility of the owning business use case.

Successful Identity responses use `ApiResponse<T>`. Application exceptions and
authentication failures are translated centrally to Problem Details. In
Development, Swagger UI is available at `/swagger`, and `/` redirects to it.

JWT settings are read from the `Jwt` configuration section. Issuer, audience,
signing key length, lifetime, signature, and token lifetime are validated; the
current bearer configuration accepts HMAC-SHA256 tokens with zero clock skew.

## Persistence and runtime topology

The runtime is a single ASP.NET Core process backed by one PostgreSQL database:

```text
HTTP client
    |
    v
Haggly.Api
    |-- Haggly.Application use cases
    |-- Haggly.Infrastructure authentication and persistence
    `-- Haggly.Domain model
    |
    v
PostgreSQL (local: localhost:5433, database: haggly)
```

The local database is defined in `docker-compose.yml`. The development
configuration uses the `HagglyDatabase` connection-string name. Database
business behavior must remain owned by the relevant module; EF Core mappings
and repositories are adapters, not business-rule owners.

## Testing structure

`Haggly.UnitTests` covers current Domain, Application, Infrastructure, Identity,
JWT, and persistence-configuration behavior. `Haggly.IntegrationTests` covers
the authentication pipeline, Identity endpoint contracts, and Swagger
contracts. The repository currently has no active architecture-test project.

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
5. Add focused unit tests and real boundary/integration tests for persistence,
   authentication, or external providers.

Cross-module workflows should have one coordinating Application use case.
Modules should communicate through explicit contracts and must not directly
mutate another module's entities.

## Proposed future state

The following is direction, not a description of files that currently exist:

- Complete Application, Infrastructure, and API slices for Markets, Catalog,
  Inventory, Negotiation, Sales, Payments, and Finance.
- Extend EF Core configuration and migrations as each module gains a persisted
  use case.
- Add architecture tests only when an actual architecture-test project and its
  solution entry are created.
- Add operational instrumentation or messaging only when a concrete MVP use
  case requires them.

The README remains the source for MVP scope and business requirements. This
document records executable structure and boundaries; it does not expand the
MVP into delivery, multi-market, or other excluded functionality.
