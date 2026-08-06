You are a senior .NET solution architect. Scaffold the initial solution structure for the Haggly project as a modular monolith designed for future microservice extraction.

Project name:
Haggly – “Bớt hong cô ơi”

Working rules:
- Inspect the repository before making changes.
- Preserve existing user files and unrelated changes.
- Use .NET 10 and C# 14.
- Create compilable scaffolding only.
- Do not implement business workflows, database tables, migrations, or REST operations yet.
- Do not introduce event sourcing or a distributed microservice architecture.
- After scaffolding, restore, build, and run all tests.
- Report any assumption or deviation.

Technology stack:
- .NET 10
- C# 14
- ASP.NET Core 10 Minimal APIs
- Entity Framework Core 10 for command-side persistence
- Dapper for query-side data access
- SQL Server as the initial relational database
- FluentValidation for application/request validation
- MediatR or an equivalent lightweight in-process command/query dispatcher
- ASP.NET Core authentication and policy-based authorization
- ProblemDetails with RFC 9457-compatible API errors
- OpenAPI
- xUnit for testing

Architecture style:
- Modular monolith
- Domain-driven bounded modules
- Clean Architecture inside each module
- Vertical Slice organization inside Application and Presentation
- Pragmatic CQRS:
  - EF Core for commands
  - Dapper for queries
  - No separate physical read database in MVP
- One deployable ASP.NET Core API host
- One relational database initially
- Separate database schema and DbContext per module
- Strict module ownership to allow later microservice extraction

Create these bounded modules:
1. Identity
2. MarketManagement
3. Catalog
4. Inventory
5. Sales
6. Payments
7. Finance

Domain ownership:

Identity:
- User
- Role
- UserRole
- BuyerProfile
- VendorProfile
- AdminProfile
- DelivererProfile

MarketManagement:
- Market
- Stall
- Vendor-to-stall ownership

Catalog:
- Category
- Product
- ProductStall

Inventory:
- InventorySession
- DailyProductListing
- InventoryReservation
- InventoryLedger

Sales:
- Order
- StallFulfillment
- OrderItem
- NegotiationSession
- NegotiationMessage
- NegotiationOffer
- NegotiationOfferItem
- Pickup workflow

Payments:
- PaymentMethod
- Payment
- PaymentTransaction
- PaymentAllocation

Finance:
- RevenueLedger
- Revenue reporting

Do not create these domain types yet. Their ownership should be documented in module README files or architecture documentation only.

Create the following repository structure:

Haggly/
├── Haggly.slnx
├── src/
│   ├── Bootstrapper/
│   │   └── Haggly.Api/
│   ├── BuildingBlocks/
│   │   ├── Haggly.SharedKernel/
│   │   ├── Haggly.Application.Abstractions/
│   │   ├── Haggly.Infrastructure/
│   │   └── Haggly.Contracts/
│   └── Modules/
│       ├── Identity/
│       ├── MarketManagement/
│       ├── Catalog/
│       ├── Inventory/
│       ├── Sales/
│       ├── Payments/
│       └── Finance/
├── tests/
│   ├── Haggly.ArchitectureTests/
│   ├── Modules/
│   └── Haggly.EndToEndTests/
├── database/
│   ├── scripts/
│   └── local/
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── api/
│   └── diagrams/
├── deploy/
│   ├── docker/
│   └── compose/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── .gitignore
└── README.md

For every business module, create these projects:

src/Modules/{Module}/
├── Haggly.Modules.{Module}.Domain/
├── Haggly.Modules.{Module}.Application/
├── Haggly.Modules.{Module}.Infrastructure/
├── Haggly.Modules.{Module}.Presentation/
├── Haggly.Modules.{Module}.Contracts/
└── README.md

For example:

src/Modules/Inventory/
├── Haggly.Modules.Inventory.Domain/
├── Haggly.Modules.Inventory.Application/
├── Haggly.Modules.Inventory.Infrastructure/
├── Haggly.Modules.Inventory.Presentation/
├── Haggly.Modules.Inventory.Contracts/
└── README.md

Layer responsibilities:

Domain:
- Aggregates
- Entities
- Value objects
- Domain services
- Domain events
- Domain errors and business rules
- No EF Core, Dapper, ASP.NET Core, logging, or infrastructure dependencies

Application:
- Commands, queries, and their handlers
- Use-case orchestration
- Validators
- Authorization requirements associated with use cases
- Repository and gateway abstractions
- Transaction abstractions
- No direct dependency on ASP.NET Core or concrete persistence

Infrastructure:
- EF Core DbContext and command persistence
- Dapper query implementations
- Repository implementations
- Outbox and inbox support
- Integration-event handlers
- External service adapters
- Scheduled/background jobs
- Module dependency registration

Presentation:
- ASP.NET Core Minimal API endpoints
- HTTP request and response models
- Endpoint filters
- HTTP authorization policies
- Mapping between HTTP contracts and application requests
- Module endpoint registration

Contracts:
- Public module API
- Cross-module commands and queries where synchronous interaction is necessary
- Integration events
- Stable DTOs needed by consumers
- No domain entities

Create only representative placeholder folders/files where required to make the architecture understandable. Do not fill every possible folder with empty files.

Use vertical slices in Application. Document the intended pattern:

Application/
├── Orders/
│   ├── CreateOrder/
│   │   ├── CreateOrderCommand.cs
│   │   ├── CreateOrderHandler.cs
│   │   └── CreateOrderValidator.cs
│   └── GetOrderDetails/
│       ├── GetOrderDetailsQuery.cs
│       ├── GetOrderDetailsHandler.cs
│       └── OrderDetailsResponse.cs
└── Abstractions/

Do not implement these Sales use cases yet; use this only as documented structure unless a minimal compile-safe example is necessary.

Building block responsibilities:

Haggly.SharedKernel:
- Base Entity
- AggregateRoot
- ValueObject
- IDomainEvent
- DomainException
- Result and Error abstractions, if used
- Keep this project extremely small

Haggly.Application.Abstractions:
- ICommand<TResult>
- IQuery<TResult>
- Command/query handler abstractions if not directly exposing MediatR
- IUnitOfWork or transaction abstractions
- ICurrentUser
- IClock
- Application pipeline abstractions

Haggly.Infrastructure:
- Shared authentication setup
- Policy-based authorization support
- Outbox/inbox infrastructure
- Serilog configuration
- OpenTelemetry configuration
- Clock implementation
- Shared persistence conventions
- Cross-cutting dependency registration

Haggly.Contracts:
- Truly global integration contracts only
- Do not put module-specific contracts here

API host responsibilities:
- Application startup and composition root
- Register all modules
- Configure authentication and authorization
- Configure ProblemDetails and global exception handling
- Configure OpenAPI
- Configure health checks
- Configure Serilog and OpenTelemetry
- Map module endpoints
- Do not place business logic in Haggly.Api
- Add a simple `/health` or standard health-check endpoint
- Add a root or version endpoint only if useful

Each module should expose a clear registration API, such as:
- AddIdentityModule(...)
- MapIdentityEndpoints(...)
- AddInventoryModule(...)
- MapInventoryEndpoints(...)

Keep these methods minimal and compile-safe. Do not create actual business endpoints.

Project dependency rules:

Domain:
- May reference Haggly.SharedKernel only

Application:
- May reference its own Domain
- May reference Haggly.Application.Abstractions
- May reference its own Contracts when justified

Infrastructure:
- May reference its own Application
- May reference its own Domain
- May reference its own Contracts
- May reference shared infrastructure/building blocks

Presentation:
- May reference its own Application
- May reference its own Contracts
- Must not reference its own Infrastructure

Contracts:
- Must remain independent of Domain and Infrastructure
- May reference minimal shared contracts only when necessary

API:
- May reference module Presentation and Infrastructure projects for composition
- Must not manipulate domain objects or module persistence directly

Cross-module rules:
- A module must not reference another module’s Domain, Application, Infrastructure, or Presentation project
- Cross-module compile-time references are allowed only to the target module’s Contracts project
- Prefer integration events for cross-module communication
- Do not share domain entities between modules
- Do not create a generic shared repository
- Do not create a single global application DbContext
- Do not allow one module to update another module’s database tables

Create architecture tests that verify at minimum:
1. Domain projects do not depend on Infrastructure.
2. Domain projects do not depend on Presentation.
3. Application projects do not depend on Infrastructure.
4. Application projects do not depend on Presentation.
5. Presentation projects do not depend directly on Infrastructure.
6. Business modules do not directly reference another module’s internal projects.
7. Contracts projects do not depend on Domain or Infrastructure.
8. Domain projects do not depend on ASP.NET Core, EF Core, or Dapper.

Test organization:

tests/
├── Haggly.ArchitectureTests/
├── Modules/
│   ├── Identity/
│   │   ├── Haggly.Modules.Identity.Domain.Tests/
│   │   ├── Haggly.Modules.Identity.Application.Tests/
│   │   ├── Haggly.Modules.Identity.IntegrationTests/
│   │   └── Haggly.Modules.Identity.Api.Tests/
│   ├── MarketManagement/
│   ├── Catalog/
│   ├── Inventory/
│   ├── Sales/
│   ├── Payments/
│   └── Finance/
└── Haggly.EndToEndTests/

To avoid excessive scaffolding, initially create:
- Architecture test project
- End-to-end test project
- Domain, Application, Integration, and API test projects for Inventory
- Domain, Application, Integration, and API test projects for Sales

Document that other module test projects should be created when implementation begins.

Database conventions:
- One database for the modular monolith
- One schema per module:
  - identity
  - market
  - catalog
  - inventory
  - sales
  - payments
  - finance
  - integration
- One EF Core DbContext per module
- Each module owns its migrations
- Do not create migrations or actual entity mappings yet
- Dapper queries normally read only tables owned by their module
- Do not implement cross-schema writes
- Do not create a global HagglyDbContext

Add documentation:

1. `README.md`
   - Project purpose
   - Technology stack
   - How to restore, build, test, and run
   - High-level module list
   - Important architecture rules

2. `docs/architecture/modular-monolith.md`
   - Explain the module boundaries
   - Explain Clean Architecture and vertical slices
   - Explain pragmatic CQRS
   - Explain database schema ownership
   - Explain domain events versus integration events
   - Explain future microservice extraction

3. `docs/architecture/dependency-rules.md`
   - List permitted project references
   - List forbidden dependencies
   - Explain how architecture tests enforce them

4. `docs/architecture/database-ownership.md`
   - Map modules to database schemas
   - State that modules cannot update another module’s tables

5. ADRs:
   - `docs/adr/0001-use-modular-monolith.md`
   - `docs/adr/0002-use-pragmatic-cqrs.md`
   - `docs/adr/0003-use-database-schema-per-module.md`
   - `docs/adr/0004-use-outbox-for-integration-events.md`

6. Module README files:
   - State module purpose
   - State owned domain concepts
   - State allowed dependencies
   - State expected integration events
   - Avoid pretending unimplemented behavior already exists

Add initial configuration:
- `global.json` targeting the installed .NET 10 SDK
- `Directory.Build.props` containing shared compiler settings
- `Directory.Packages.props` containing centrally managed package versions
- `.editorconfig`
- Appropriate `.gitignore`
- Development appsettings template without secrets
- `appsettings.Local.json` excluded from Git if used
- Never commit passwords, API keys, tokens, or connection-string credentials

Use project names and namespaces consistently:
- Haggly.Api
- Haggly.SharedKernel
- Haggly.Application.Abstractions
- Haggly.Infrastructure
- Haggly.Contracts
- Haggly.Modules.{Module}.{Layer}

Namespaces should follow folder and project ownership, for example:
- Haggly.Modules.Inventory.Domain
- Haggly.Modules.Inventory.Application.Reservations.ReserveStock
- Haggly.Modules.Inventory.Infrastructure.Persistence
- Haggly.Modules.Inventory.Presentation.Endpoints
- Haggly.Modules.Inventory.Contracts.IntegrationEvents

Do not:
- Implement domain entities or workflows
- Add controllers; use Minimal APIs
- Add database migrations
- Add business tables
- Add sample products, orders, users, or seed data
- Add a frontend
- Add Docker infrastructure unless only a minimal placeholder is needed
- Add Kubernetes
- Split the application into deployable microservices
- Introduce MassTransit, Kafka, RabbitMQ, Redis, or a service bus yet
- Create unnecessary empty interfaces
- Create a generic repository abstraction
- Put business logic in endpoints or the API bootstrapper
- Put module-specific types in shared building blocks
- Depend on preview package versions when stable .NET 10-compatible versions exist

Execution:
1. Inspect the current repository and installed .NET SDK.
2. Summarize existing files and identify anything that must be preserved.
3. Create or update the solution and project structure.
4. Add project references according to the dependency rules.
5. Add only the packages needed for the scaffold to compile and test.
6. Add minimal module registration and endpoint-mapping entry points.
7. Add architecture documentation and ADRs.
8. Add architecture tests.
9. Run:
   - dotnet restore
   - dotnet build --no-restore
   - dotnet test --no-build
10. Fix all build and test failures caused by the scaffolding.
11. Inspect `git diff` and ensure no unrelated files were modified.

Acceptance criteria:
- The complete solution restores successfully.
- The solution builds with zero errors.
- All tests pass.
- All seven business modules exist.
- Every module has Domain, Application, Infrastructure, Presentation, and Contracts projects.
- Project references follow the specified dependency direction.
- The API host can start and compose every module.
- The health endpoint is mapped.
- No business domain behavior has been implemented.
- No database migration or business table has been created.
- Architecture tests enforce the most important dependency rules.
- Documentation clearly explains module ownership and future extraction.
- No secrets or machine-specific absolute paths are committed.

At completion, provide:
- A concise summary of created files and projects
- The resulting solution tree
- Build and test results
- Any assumptions made
- Any deviations from this prompt and why
- Recommended next implementation step, without implementing it