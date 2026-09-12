# Haggly Backend Agent Guide

## Scope

The backend is a .NET 10 modular monolith. Backend source, tests, migrations,
database assets, solution files, and API contracts live under this directory.
Read the root `../AGENTS.md` first. This file governs backend-only work.

## Start here

1. Read `ARCHITECTURE.md` when the change affects structure, dependencies, or a
   cross-module workflow.
2. Route the business concern to the guide below.
3. Inspect `global.json`, shared props, `Haggly.slnx`, affected projects, current
   implementation, and corresponding tests before choosing commands.
4. Implement the smallest complete vertical slice through only the necessary
   layers.
5. Apply the shared workflow in `../docs/agent-guides/engineering-harness.md`.

## Business routing

The guides in `docs/agent-guides/` are verified caches of backend-local
knowledge. Missing or empty guidance grants no permission to invent behavior.

| Concern | Owner | Read next |
|---|---|---|
| Users, profiles, roles, authentication | Identity | `docs/agent-guides/identity.md` |
| Markets, stalls, vendors, ownership | Markets | `docs/agent-guides/markets.md` |
| Categories and reusable product definitions | Catalog | `docs/agent-guides/catalog.md` |
| Availability, quantities, listings, reservations | Inventory | `docs/agent-guides/inventory.md` |
| Orders, carts, POS, negotiation, pickup/fulfillment | Sales | `docs/agent-guides/sales.md` |
| Collection, payment status, allocation | Payments | `docs/agent-guides/payments.md` |
| Revenue, earnings, ledger, reporting | Finance | `docs/agent-guides/finance.md` |
| EF Core, Dapper, mappings, migrations | Persistence plus business owner | `docs/agent-guides/persistence.md` |
| Routes, middleware, Problem Details, OpenAPI | API plus business owner | `docs/agent-guides/api.md` |

Ambiguity rules:

- Product identity/category belongs to Catalog; availability and quantity
  belong to Inventory.
- Collection/status/allocation belongs to Payments; recognized revenue and
  reporting belong to Finance.
- Pickup belongs to Sales/Fulfillment unless executable code proves otherwise.
- Cross-module workflows have one coordinating Application use case; modules
  do not directly mutate one another's entities.

## Layer routing

Inspect business behavior in this order:

1. `src/Haggly.Domain`: state, invariants, transitions, and domain errors.
2. `src/Haggly.Application`: use cases, validation, authorization, orchestration,
   and narrow external ports.
3. `src/Haggly.Infrastructure`: persistence, messaging, authentication, and
   provider adapters.
4. `src/Haggly.Api`: HTTP mapping, middleware, policies, and OpenAPI.
5. `tests`: focused business tests and available real-boundary tests.

Allowed project dependencies are documented in `ARCHITECTURE.md`. Do not put
business decisions in endpoints, EF mappings, Dapper adapters, consumers, or
provider integrations.

## Object-oriented design

- Keep invariants valid through construction and intention-revealing state
  transitions; avoid public mutable state.
- Give each type one cohesive responsibility and keep interfaces narrow and
  capability-oriented.
- Add an abstraction only for an observed boundary, consumer capability, or
  real variation—not for speculative extensibility.
- Prefer composition over inheritance and explicit dependencies over service
  location.
- Application policy depends on Domain concepts and ports; infrastructure
  details do not leak into Domain or Application contracts.

## Persistence naming

Every concrete Infrastructure type that directly executes database queries or
commands must end in `Repository`. Include `Ef` or `Dapper` when it clarifies
the adapter. Direct database adapters must not end in `Query`, `Command`,
`Catalog`, `Store`, or `UnitOfWork` when touched; transaction-only coordinators
end in `TransactionExecutor`. Application abstractions remain
capability-oriented and provider-neutral.

## Backend tests

Test Haggly-owned behavior at the lowest layer that proves it. Tests are
required when they provide meaningful confidence for domain invariants,
calculations, money, inventory, state transitions, authorization, critical
workflows, bug regressions, or real persistence/authentication/messaging/HTTP
boundaries.

New unit tests use visible Arrange, Act, and Assert sections, fresh deterministic
state, and `Method_Scenario_ExpectedResult` names. Domain tests use real Domain
objects. Application tests use real handlers and substitute only Application
ports. Do not test framework behavior, trivial containers, or pass-through
wiring solely to increase coverage.

## Verification ladder

Run checks sequentially and stop at the first failure:

1. Build the smallest affected project.
2. Run the new or directly affected test.
3. Run the affected test class or module filter.
4. Run real-boundary tests when the change crosses that boundary and the suite
   exists.
5. Leave broader suites to pull-request or release CI unless requested.

Full CI/release commands are:

```powershell
dotnet restore backend/Haggly.slnx
dotnet build backend/Haggly.slnx --no-restore
dotnet test backend/Haggly.slnx --no-build
```

When working from `backend/`, omit the `backend/` prefix. Never retry a failed
test without first classifying the failure from its assertion, exception, logs,
and implementation. Report every command actually run and every limitation.

## Completion

A backend change is complete when behavior, risk-selected tests, project
boundaries, HTTP contracts, migrations, configuration, and affected guides are
consistent. State why no new test was needed when the change has low behavioral
risk or existing coverage is sufficient.
