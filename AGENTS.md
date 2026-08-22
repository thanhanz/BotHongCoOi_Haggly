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

Module-specific guides under `docs/agent-guides/` have not been implemented yet.

Until they exist:

- Use this root `AGENTS.md` for repository-wide implementation rules.
- Use `ARCHITECTURE.md` for architecture and module ownership.
- Do not search for or depend on nonexistent module guides.
- Derive new conventions from the first validated vertical slices before
  documenting them as reusable guidance.


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

## Risk-based testing and verification

Test behavior Haggly owns. Do not test framework internals, library behavior,
trivial data containers, or implementation details. Test each behavior at the
lowest layer that can prove it. Do not repeat the same business scenario at
multiple layers unless each test covers a distinct risk.

Strict red-green-refactor TDD is required for:

- Domain invariants, calculations, policies, and state transitions.
- Critical MVP workflows involving inventory, orders, payments, authorization,
  or cross-module consistency.
- Bug fixes, using a regression test that reproduces the defect.

For these changes:

1. Add the smallest test that proves the behavior, named
   `Method_Scenario_ExpectedResult`.
2. Run it and confirm it fails for the expected reason.
3. Implement the smallest production change.
4. Run the focused test again and refactor while it remains passing.

Choose the test layer by responsibility:

- Domain unit tests prove business rules without infrastructure.
- Application unit tests prove meaningful orchestration, authorization
  decisions, and failure handling; do not test pass-through handlers.
- Integration tests prove boundaries Haggly depends on, including PostgreSQL
  behavior, transactions, EF/Dapper mappings, authentication pipelines, and
  provider adapters.
- API contract tests prove routes, binding, authorization metadata, status
  codes, and public response shapes without duplicating domain scenarios.
- End-to-end, concurrency, load, and stress tests are added intentionally for
  named critical journeys or measured risks, not as default feature coverage.

A new test is normally unnecessary for DTO-only changes, trivial property
mapping, pass-through queries, framework wiring already covered by a boundary
test, or refactoring with unchanged observable behavior. When omitting tests,
state why existing coverage and verification are sufficient.

Use this verification ladder and stop at the first failure:

1. Build the smallest affected project to catch compilation and nullable errors.
2. Run the new or directly affected test.
3. Run the affected test class or module filter.
4. Run integration tests only when the change crosses a real boundary.
5. Leave the complete unit and integration suites to pull-request or release CI.

Run verification commands sequentially by default so failures retain a clear
cause. This does not require disabling xUnit's normal parallel execution inside
the fast unit-test project. Integration tests remain non-parallel because they
share real infrastructure.

Never retry a failed test without first reading its assertion, exception, logs,
and relevant implementation. Determine whether the cause is production
behavior, test setup, environment, or an unstable test, then fix the root cause.
Never loosen assertions, remove important scenarios, add arbitrary delays, or
disable tests merely to make CI pass.

Report every command actually run, its outcome, broader suites left to CI, and
any verification limitation. Never claim an unrun or retried suite passed.

Do not delete existing tests during unrelated feature work. Audit test removal
separately and classify candidates as critical MVP behavior, unique domain or
boundary protection, duplicate coverage, obsolete behavior, or tests coupled
only to implementation details. Remove a test only when its production behavior
is removed or equivalent protection remains at a more appropriate layer. Being
outside the MVP is not sufficient when the behavior still exists in production.

## Workspace hygiene

Create only task-required deliverables: production code, tests, required
migrations or generated source, required configuration, and directly affected
documentation. Builds and tests may create the repository's already-ignored
`bin/` and `obj/` directories. Do not create repository-local `.build`,
`.tools`, `.dotnet`, `artifacts`, `TestResults`, coverage, log, report, cache,
download, SDK-home, package-cache, or scratch paths unless the user explicitly
requests one as a deliverable. Do not add ignore rules merely to hide such
artifacts. Follow the cleanup and final-audit workflow in
`docs/agent-guides/engineering-harness.md`.


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
CI before choosing commands. Confirm referenced projects exist. Full CI/release
ladder:

```powershell
dotnet restore Haggly.slnx
dotnet build Haggly.slnx --no-restore
dotnet test Haggly.slnx --no-build
```

For local verification, follow the risk-based ladder above instead of running
the full solution suite by default. Run focused project tests first. For
persistence, authentication, transactions, or providers, use integration tests
that exercise the real boundary. The complete unit and integration suites are
pull-request or release CI gates.

When a persistence model changes, use the migration command documented in
`docs/agent-guides/persistence.md`:

```powershell
dotnet ef migrations add CreateMarketAndStallEntities `
    --project src\Haggly.Infrastructure\Haggly.Infrastructure.csproj `
    --startup-project src\Haggly.Api\Haggly.Api.csproj `
    -- `
    --connection "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
```

## Completion

Finish only when behavior, risk-selected tests, boundaries, contracts,
migrations, configuration, and documentation are consistent. Report changed
behavior, omitted-test justification, files, exact checks run, failures or
skipped checks, and remaining risks.
