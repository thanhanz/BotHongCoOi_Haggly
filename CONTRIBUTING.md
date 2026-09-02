# Haggly Team Development Guide

This guide is required reading for contributors. It describes how to set up a
new development environment, create changes, validate them, and submit work to
the team.

## 1. First-time setup

Install the following tools before starting:

- Git
- .NET SDK `10.0.201`
- Docker Desktop with Docker Compose

Clone the repository and enter the project directory:

```powershell
git clone <repository-url>
Set-Location Haggly
```

Restore the repository tools and install the local Git hooks:

```powershell
dotnet tool restore
dotnet husky install
```

The local tools are pinned in `.config/dotnet-tools.json`:

- `dotnet-ef` for Entity Framework tooling
- Husky.Net for Git hooks
- CommitLint.Net for commit-message validation

Start PostgreSQL for local development and restore the solution:

```powershell
docker compose up -d postgres
dotnet restore Haggly.slnx
```

The development database uses the `HagglyDatabase` connection string from
`src/Haggly.Api/appsettings.Development.json` and PostgreSQL port `5433`.
Development credentials must not be reused in production.

Future functional tests must use a separate `haggly_test` database and may read
`HAGGLY_TEST_CONNECTION_STRING`; the configured database name must remain
`haggly_test`.

In Development, the API exposes Swagger at `/swagger` and redirects `/` to the
Swagger UI.

## 2. Branch and pull-request rules

Never implement work directly on `develop` or `main`. Do not push directly to
either branch.

For every task:

1. Start from an up-to-date `develop` branch.
2. Create a new branch for the task.
3. Make commits only on that branch.
4. Push the branch to the remote.
5. Open a pull request targeting `develop`.
6. Address review comments and keep the branch updated with `develop`.
7. Merge only after required CI checks and approvals pass.

Suggested branch names:

```text
feature/<short-description>
fix/<short-description>
refactor/<short-description>
docs/<short-description>
chore/<short-description>
```

Example:

```powershell
git fetch origin
git switch develop
git pull --ff-only origin develop
git switch -c feature/identity-login
```

Normal feature work targets `develop`. The `main` branch is also protected by
CI and is reserved for the project's release or integration process.

## 3. Commit-message rules

Every commit must use this format:

```text
<type>(<scope>): <message>
```

Examples:

```text
feat(identity): add buyer registration
fix(api): return problem details for invalid tokens
test(inventory): cover reservation release
docs(contributing): add branch workflow
```

The scope is required and must not be empty. The subject must be no longer than
90 characters. Allowed types are:

```text
feat fix refactor build chore style test docs perf revert ci
```

Husky runs the `commit-msg` hook locally. The hook rejects an invalid message
before the commit is created, and CommitLint.Net performs the detailed
Conventional Commits validation. The CI pipeline repeats this validation for
every commit in a pull request.

Do not bypass the hook with `git commit --no-verify` except for a documented
emergency. A bypassed commit will still fail the pull-request CI check.

## 4. Development workflow

Before editing, read:

- `AGENTS.md` for repository-wide engineering rules.
- `ARCHITECTURE.md` for current project boundaries and implementation state.
- `README.md` for MVP requirements and business rules.
- The nearest existing implementation and its tests.

For a new behavior or behavior change, follow test-first development:

1. Add or update the focused test cases.
2. Run the tests and confirm the new test fails for the expected reason.
3. Implement the smallest complete change.
4. Run the tests again.
5. Refactor while keeping the tests passing.

Keep changes focused on the task. Do not include unrelated cleanup, package
upgrades, formatting changes, or speculative abstractions in the same PR.

## 5. Architecture and ownership rules

Haggly is a .NET modular monolith. Keep business behavior in the correct
layer:

- Domain: entities, value objects, invariants, state transitions, and business
  rules. Domain must not depend on ASP.NET Core, EF Core, Dapper, or providers.
- Application: use cases, orchestration, validation, authorization needs, and
  external dependency contracts.
- Infrastructure: EF Core mappings, repositories, authentication, hashing, and
  provider adapters.
- API: HTTP routes, request/response mapping, middleware, Problem Details, and
  OpenAPI configuration.

Use the business module as the owner of behavior. Current module ownership is:

- Identity: users, profiles, roles, and authentication-related contracts.
- Markets: markets, stalls, vendors, and ownership.
- Catalog: categories and reusable product definitions.
- Inventory: daily sessions, stock, listings, and reservations.
- Negotiation: negotiation sessions, offers, and messages.
- Sales: orders and stall fulfillment.
- Payments: collection, payment status, transactions, and allocation.
- Finance: revenue and financial reporting records.

Do not put business decisions in endpoints, EF mappings, or provider adapters.
For cross-module behavior, use one coordinating Application use case. Do not
directly mutate another module's entities.

## 6. Testing and verification

The primary suite is `Haggly.UnitTests`:

- `Domain` tests use real entities and aggregates without mocks.
- `Application` tests use real handlers and Domain objects; NSubstitute replaces
  only Application ports.
- Every test follows Arrange/Act/Assert and
  `Method_Scenario_ExpectedResult` naming.
- Tests use deterministic fresh data and do not depend on execution order.

Run the focused module or class while developing, then the complete active unit suite:

```powershell
dotnet test tests/Haggly.UnitTests/Haggly.UnitTests.csproj --filter "FullyQualifiedName~Inventory"
dotnet test tests/Haggly.UnitTests/Haggly.UnitTests.csproj
```

Before opening or updating a PR, run the full local verification ladder:

```powershell
dotnet restore Haggly.slnx
dotnet build Haggly.slnx --no-restore
dotnet test Haggly.slnx --no-build
```

Persistence, authentication, transaction, messaging, and provider changes
require tests at the real boundary where practical. NSubstitute-based unit tests
do not prove database, HTTP, broker, or provider behavior. A dedicated
`Haggly.FunctionalTests` project is planned but does not exist yet.

## 7. Pull-request checklist

Before requesting review, confirm:

- The work is on a task branch, not `develop` or `main`.
- The pull request targets `develop`.
- Every commit follows `<type>(<scope>): <message>`.
- Husky hooks are installed and local checks pass.
- Relevant active unit tests and required real-boundary tests were added or updated.
- The full build and test ladder passes locally.
- API contracts, configuration, migrations, and documentation are updated when
  affected.
- No secrets, passwords, tokens, generated `bin/` or `obj/` output, or local
  database files are included.
- The PR description explains the behavior changed, verification performed,
  and any remaining risks.

## 8. CI and merge requirements

Pull requests targeting `develop` or `main` are intended to run the CI workflow
in this order:

```text
commit lint -> build -> unit tests -> boundary tests -> publish
```

The checked-in workflow already runs `Haggly.UnitTests`. Its boundary-test stage
does not yet target a valid functional-test project. Update that stage to
`Haggly.FunctionalTests` when the project is introduced; do not report the stage
as passing while its project path is absent.

The publish stage produces the `haggly-api` artifact; it is not a production
deployment. A PR must have passing CI and the required team approvals before it
is merged.

If a local hook and CI disagree, treat CI and the checked-in configuration as
the source of truth and fix the branch before requesting merge.
