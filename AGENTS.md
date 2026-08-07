# Haggly Agent Map

## Purpose

Haggly is intended to be a .NET 10 modular monolith. Use this file as a map,
not as proof that planned code exists. Ground every change in the current
workspace and implement the smallest complete vertical slice.

## Start here

1. Run `git status --short`; preserve unrelated user changes.
2. Classify the request: explanation, bug, feature, refactor, API, persistence,
   architecture, or documentation.
3. Locate relevant files with `rg --files` and targeted `rg` searches.
4. Read the governing source below, affected projects, nearest implementation,
   and corresponding tests before editing.
5. Follow the detailed workflow in `docs/agent-guides/engineering-harness.md`.

Never infer that a documented path, type, dependency, or test exists. Never
invent command output or claim unrun verification. Treat empty guides as absent
information. When material facts are uncertain, distinguish observed, inferred,
and proposed statements.

## Sources of truth

Use this order when sources conflict:

1. Current user request and explicit acceptance criteria.
2. Executable behavior: tests, compiled contracts, runtime configuration.
3. Current implementation and project references.
4. `README.md`: MVP scope, actors, functional requirements, business rules.
5. `ARCHITECTURE.md`: intended boundaries, layers, and technology direction.
6. `docs/agent-guides/*.md`: verified module-local knowledge only.

Do not silently resolve a material conflict; report it and state which source
guided the change.

## Business routing

| Concern | Owner | Read next | Expected roots |
|---|---|---|---|
| Users, profiles, roles, authentication | Identity | `docs/agent-guides/identity.md` | `Modules/Identity`, `Infrastructure/Authentication` |
| Markets, stalls, vendors, ownership | Markets | `docs/agent-guides/markets.md` | `Modules/Markets` |
| Categories and reusable product definitions | Catalog | `docs/agent-guides/catalog.md` | `Modules/Catalog` |
| Daily sessions, stock, listings, reservations | Inventory | `docs/agent-guides/inventory.md` | `Modules/Inventory` |
| Orders, POS, negotiation, pickup/fulfillment | Sales | `docs/agent-guides/sales.md` | `Modules/Sales` |
| Collection, payment status, allocation | Payments | `docs/agent-guides/payments.md` | `Modules/Payments`, `Infrastructure/Payments` |
| Revenue, earnings, ledger, reporting | Finance | `docs/agent-guides/finance.md` | `Modules/Finance` |
| EF Core, Dapper, mappings, migrations | Persistence + business owner | `docs/agent-guides/persistence.md` | `Infrastructure/Persistence`, `database` |
| HTTP, middleware, Problem Details, OpenAPI | API + business owner | `docs/agent-guides/api.md` | `Haggly.Api` |

Code roots are under `src/Haggly.Domain`, `src/Haggly.Application`,
`src/Haggly.Infrastructure`, and `src/Haggly.Api`. Expected roots are routing
targets, not evidence that directories already exist.

Ambiguity rules:

- Product identity/category -> Catalog; availability/quantity/listing -> Inventory.
- Collection/status/allocation -> Payments; recognized revenue/reporting -> Finance.
- Pickup -> Sales/Fulfillment unless current code proves another owner.
- Transport or storage does not own business behavior.
- Cross-module workflows have one coordinating Application use case; modules do
  not directly mutate one another's entities.

## Layer routing

For business behavior, inspect in this order:

1. Domain: invariants, state transitions, domain errors.
2. Application: use case, validation, authorization, external ports.
3. Infrastructure: persistence or provider adapters only when required.
4. API: HTTP contract and error translation only when required.
5. Tests: focused unit tests and boundary/integration tests.

Do not put business decisions in endpoints, EF mappings, or adapters.

## Boundaries

Allowed dependencies:

- API -> Application, Infrastructure
- Application -> Domain
- Infrastructure -> Application, Domain
- Domain -> no Haggly project

Domain must not reference ASP.NET Core, EF Core, Dapper, or providers. Avoid
generic repositories, vague manager/service abstractions, and speculative
microservices, brokers, event sourcing, CQRS infrastructure, or outbox work.
Preserve public contracts unless the request explicitly changes them.

## Verification

Inspect `global.json`, shared props, `Haggly.slnx`, affected `.csproj` files, and
CI before choosing commands. Confirm referenced projects exist. Preferred ladder:

```powershell
dotnet restore Haggly.slnx
dotnet build Haggly.slnx --no-restore
dotnet test Haggly.slnx --no-build
```

Run focused project tests first. For persistence, authentication, transactions,
or providers, use integration tests that exercise the real boundary.

## Completion

Finish only when behavior, relevant tests, boundaries, contracts, migrations,
configuration, and documentation are consistent. Report changed behavior,
files, exact checks run, failures or skipped checks, and remaining risks.
