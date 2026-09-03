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

Module-specific guides under `docs/agent-guides/` are verified caches of local
knowledge. Some guides remain empty; treat an empty guide as absent and use this
root file plus `ARCHITECTURE.md`. Never infer a convention from an expected path
that has no implementation or guide content.


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

## Object-oriented design and SOLID

Use object-oriented design to keep business behavior explicit and changes
local. Apply SOLID as design guidance, not as a reason to create layers,
interfaces, or patterns that the current behavior does not need.

- Give each type one cohesive responsibility and place behavior with the state
  and rules it governs. Prefer intention-revealing methods over public setters
  and anemic domain objects.
- Keep invariants valid through construction and state transitions. Encapsulate
  mutable state and expose the smallest useful public API.
- Extend behavior through focused composition or polymorphism when a real
  variation exists. Do not add speculative inheritance hierarchies, strategy
  interfaces, factories, or extension points.
- Derived implementations must honor the contract and expectations of the
  abstraction they implement. Do not weaken validation or change observable
  semantics unexpectedly.
- Keep interfaces small and capability-oriented. Create an interface at a real
  boundary or when multiple behaviors/substitution are required, not for every
  class.
- Make high-level Application policy depend on Domain concepts and narrow
  ports; keep EF Core, Dapper, HTTP, and provider details in Infrastructure or
  API adapters.
- Prefer composition over inheritance, explicit dependencies over service
  location, and cohesive types over generic `Manager`, `Helper`, or `Service`
  classes.

Before introducing an abstraction, identify the concrete responsibility or
variation it isolates. Choose the simplest design that satisfies current
requirements, and refactor when evidence shows responsibilities or reasons to
change have diverged.

## Risk-based testing and verification

Test behavior Haggly owns. Do not test framework internals, library behavior,
trivial data containers, or implementation details. Test each behavior at the
lowest layer that can prove it. Do not repeat the same business scenario at
multiple layers unless each test covers a distinct risk.

TDD is encouraged when it helps clarify behavior, but a strict
red-green-refactor sequence is not required. Tests are required when they give
meaningful confidence in Haggly-owned behavior or data correctness, especially:

- Accuracy-sensitive domain invariants, calculations, policies, state
  transitions, money, quantity, allocation, and cross-module consistency.
- Critical workflows involving inventory, orders, payments, authorization, or
  other behavior where an incorrect result can corrupt or expose data.
- Bug fixes, preferably with a regression test that reproduces the defect when
  the behavior can be tested reliably at a useful layer.
- Real integration and technology boundaries Haggly relies on, including
  database constraints, transactions, EF/Dapper mappings and queries,
  serialization, authentication, and external provider adapters.

For these changes:

1. Add the smallest test that proves the behavior, named
   `Method_Scenario_ExpectedResult`.
2. Implement the smallest production change; writing and running the test first
   is optional unless the user explicitly requests strict TDD.
3. Run the focused test and confirm it proves the intended behavior at the
   appropriate layer.
4. Refactor only while the relevant tests remain passing.

Choose the test layer by responsibility:

- Domain tests in `tests/Haggly.UnitTests/Domain` use real Domain objects and
  prove invariants, calculations, and state transitions without mocks or DI.
- Application tests in `tests/Haggly.UnitTests/Application` use real handlers
  and Domain objects. Substitute only Application ports with NSubstitute to
  prove meaningful orchestration, authorization decisions, and failure handling;
  do not test pass-through handlers.
- Boundary and future functional tests prove dependencies Haggly relies on, including PostgreSQL
  behavior, transactions, EF/Dapper mappings, authentication pipelines, and
  provider adapters.
- API contract tests prove routes, binding, authorization metadata, status
  codes, and public response shapes without duplicating domain scenarios.
- End-to-end, concurrency, load, and stress tests are added intentionally for
  named critical journeys or measured risks, not as default feature coverage.

A new test is normally unnecessary for DTO-only changes, trivial in-memory
property assignment, pass-through code, framework wiring already covered by a
boundary test, or refactoring with unchanged observable behavior. Do not add
tests only to increase test counts or coverage percentages. Mapping or query
behavior that depends on EF Core, Dapper, SQL, serialization, or provider
conventions is not trivial and should be proven at the real boundary. When
omitting tests, state why the change has low behavioral risk or which existing
coverage is sufficient.

Every new unit test uses visible Arrange, Act, and Assert sections, is named
`Method_Scenario_ExpectedResult`, creates fresh deterministic state, and must not
depend on execution order or shared mutable fixtures.

Use this verification ladder and stop at the first failure:

1. Build the smallest affected project to catch compilation and nullable errors.
2. Run the new or directly affected test.
3. Run the affected test class or module filter.
4. Run functional tests when the change crosses a real boundary and that suite exists.
5. Leave broader functional suites to pull-request or release CI.

Run verification commands sequentially by default so failures retain a clear
cause. This does not require disabling xUnit's normal parallel execution inside
the fast unit-test project. Real-boundary tests remain non-parallel when they
share infrastructure.

Never retry a failed test without first reading its assertion, exception, logs,
and relevant implementation. Determine whether the cause is production
behavior, test setup, environment, or an unstable test, then fix the root cause.
Never loosen assertions, remove important scenarios, add arbitrary delays, or
disable tests merely to make CI pass.

Report every command actually run, its outcome, broader suites left to CI, and
any verification limitation. Never claim an unrun or retried suite passed.

Do not delete tests during unrelated feature work. Remove a test only when its
behavior is gone, duplicated, obsolete, or protected at a more appropriate layer.

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

## Persistence naming

- Every concrete Infrastructure class that directly connects to or executes a
  query or command against a database must have a name ending in `Repository`.
- Include the persistence technology prefix when it clarifies the adapter, for
  example `EfInventoryPaymentRepository` or `DapperInventoryRepository`.
- Do not name a direct database adapter with suffixes such as `Query`, `Command`,
  `Catalog`, `Store`, or `UnitOfWork`; rename existing implementations to the
  `Repository` convention when they are touched or as a dedicated refactor.
  Transaction-only coordinators are the exception and must end in
  `TransactionExecutor`.
- This suffix rule applies to concrete database-access classes. Application
  abstractions should remain capability-oriented and must not expose EF Core,
  Dapper, SQL, connection, or provider details.
- A repository may be read-only, write-only, or both. Keep its interface narrow
  and business-focused; the `Repository` suffix does not justify creating a
  generic repository.

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
the full solution suite by default. Run `tests/Haggly.UnitTests` first. For
persistence, authentication, transactions, messaging, HTTP, or providers, add
coverage to `Haggly.FunctionalTests` once that project exists; do not simulate
those boundaries in the unit project.

```powershell
dotnet build tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-restore
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-build
```

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
