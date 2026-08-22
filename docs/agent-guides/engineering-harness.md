# Haggly Engineering Harness

This document contains the detailed operating protocol referenced by the root
`AGENTS.md`. The root file stays short so it can act as a high-signal routing
map in agent context.

## Grounding contract

Before editing:

1. Read the root `AGENTS.md` and run `git status --short`.
2. Determine the request type, owning module, governing requirement, and
   affected layers.
3. Use `rg --files` and targeted `rg` searches to confirm referenced paths,
   projects, symbols, callers, and tests exist.
4. Read affected files completely enough to understand their contracts.
5. Find one nearby working implementation and its tests.
6. Define the smallest observable behavior that satisfies the request.

Evidence rules:

- A proposed tree in `ARCHITECTURE.md` is direction, not implemented fact.
- Empty files and missing paths provide no evidence.
- Existing executable behavior outranks descriptive documentation when they
  disagree, unless the user's request explicitly changes that behavior.
- General .NET habits do not establish a Haggly convention.
- Do not invent classes, endpoints, database objects, configuration, commands,
  or test results.
- State meaningful conclusions as observed, inferred, or proposed when they
  could otherwise be confused.
- Show material conflicts with file evidence. Ask only when the choice changes
  behavior significantly; otherwise use the smallest reversible interpretation
  and disclose it.

## Request modes

| Mode | Operating rule |
|---|---|
| Explain, review, diagnose | Inspect and report; do not modify code unless asked. |
| Bug fix | Identify the failing path and owning rule, add the smallest regression test, make the smallest fix. |
| New behavior | Find the requirement and business rule, then implement one vertical slice using the root risk-based test policy. |
| Refactor | Preserve observable behavior, establish coverage first, avoid feature work. |
| API change | Inspect business owner, application contract, validation, authorization, OpenAPI, integration tests. |
| Persistence change | Inspect ownership, application port, mapping/query, transaction, migration, integration tests. |
| Architecture change | Read all of `ARCHITECTURE.md`, inspect references, verify dependency boundaries. |
| Documentation | Verify every statement against code/configuration; label future plans. |

Product ownership wins over transport or storage. An Inventory endpoint remains
Inventory-owned and merely exposed through API. A database change is owned by
Persistence and the affected business module.

For cross-module behavior, select one coordinating Application use case. Other
modules expose explicit behavior or contracts; the coordinator must not mutate
their entities directly.

## Implementation record

Before a non-trivial edit, be able to fill in:

```text
Request type:
Owning module:
Governing requirement or rule:
Observed implementation path:
Affected layers:
Nearest precedent:
Tests to add or change:
Verification commands:
Open assumptions or conflicts:
```

Keep this in working notes unless it helps the user review a consequential
decision. If no implementation path or precedent exists, say that the area is
scaffolded. Establish the smallest convention consistent with
`ARCHITECTURE.md`; do not claim it was already present.

## Editing rules

- Keep the diff inside the smallest valid module and layer boundary.
- Put invariants and state transitions in Domain.
- Put orchestration, validation, authorization requirements, and external ports
  in Application.
- Put persistence and provider implementations in Infrastructure.
- Put only transport mapping and public HTTP concerns in API.
- Follow a local pattern only after locating it in current code.
- Do not add unrelated cleanup, formatting, package upgrades, or abstractions.
- Do not edit generated files directly or expose secret values.
- Update public contracts, migrations, configuration, and documentation when
  behavior affects them.
- Update a module guide when the change establishes or invalidates durable
  module knowledge.

## Repository artifact hygiene

Implementation work must leave only task-required deliverables in the
repository. Allowed tracked changes are production source, test source,
required migrations or other generated source, required configuration, and
directly affected documentation. Do not create an additional file merely to
record working notes, command output, analysis, test results, or a completion
report.

Build and test commands may create the repository's existing ignored `bin/`
and `obj/` directories. Unless the user explicitly requests one as a
deliverable, do not create repository-local paths for:

- `.build`, `.tools`, `.dotnet`, `artifacts`, or `TestResults`;
- coverage output, logs, reports, downloads, or scratch data;
- a .NET CLI home, SDK installation, NuGet/package cache, or tool cache;
- redirected build output or intermediate output outside the standard ignored
  `bin/` and `obj/` paths.

Use the operating system's temporary directory for unavoidable temporary data
and remove data created there when it is no longer needed. Use an existing
tool manifest only when the task requires it; do not create a tool manifest or
local tool folder solely to run verification. Do not change `.gitignore` to
conceal a generated path.

Preserve the initial `git status --short` as the ownership baseline. Before
finishing:

1. Run `git status --short` again and account for every changed or new path.
2. Search the repository for prohibited artifact paths created during the
   task.
3. Remove only artifacts created by the current task. Never delete or clean a
   pre-existing ignored, modified, or untracked user path.
4. Report remaining changes by category: source, tests, migrations or generated
   source, configuration, and documentation.

## Verification protocol

Discover verification rather than trusting stale commands. Inspect:

- `global.json`;
- `Directory.Build.props` and `Directory.Packages.props`;
- `Haggly.slnx` and affected `.csproj` files;
- CI workflows and repository scripts;
- the relevant test projects.

Confirm every project referenced by a solution or command exists. Run commands
sequentially and stop at the first failure so its cause remains clear. For local
verification:

1. Build the smallest affected project to catch compilation and nullable errors.
2. Run the new or directly affected test.
3. Run the affected test class or module filter.
4. Run integration tests only for a real boundary or measured integration risk.

Do not invent a lint command when the repository has none. Use the configured
build and analyzers as the compilation/type check. Leave the complete unit and
integration suites to pull-request or release CI unless the user explicitly
requests them locally.

Full CI/release ladder when the workspace supports it:

```powershell
dotnet restore Haggly.slnx
dotnet build Haggly.slnx --no-restore
dotnet test Haggly.slnx --no-build
```

Focused examples:

```powershell
dotnet test tests/Haggly.UnitTests/Haggly.UnitTests.csproj
dotnet test tests/Haggly.IntegrationTests/Haggly.IntegrationTests.csproj
```

Use real boundary tests for EF Core, Dapper, database constraints,
transactions, authentication, and external adapters. Mocks cannot prove
provider behavior.

Add end-to-end, concurrency, load, or stress tests only for a named critical
journey or measured risk with an explicit acceptance criterion. Do not disable
xUnit parallelism for fast unit tests merely to make commands sequential;
integration tests may remain non-parallel when they share real infrastructure.

Never retry a failed test blindly. Read the assertion, exception, logs, and
relevant implementation, classify the failure as production behavior, test
setup, environment, or instability, and fix the root cause. Never loosen an
assertion, remove an important scenario, add an arbitrary delay, or disable a
test merely to make CI pass.

Never transform "not run," "not discovered," "unavailable," or a pre-existing
failure into a passing result. Report the command, outcome, and limitation.

## Current scaffold caveats

These observations were true when this guide was created and must be rechecked,
not assumed:

- `Haggly.slnx` referenced
  `tests/Haggly.ArchitectureTests/Haggly.ArchitectureTests.csproj`, but that
  project was absent.
- `src/Haggly.Api/Program.cs` was empty.
- Module-local agent guides were empty placeholders.

If still true, report them as pre-existing limitations. Do not invent missing
implementation or claim the full solution passed.

## Definition of done

A code change is complete only when:

- requested observable behavior is implemented in the owning module;
- governing rules and important assumptions are preserved or disclosed;
- risk-selected tests required by the root policy exist, and omitted tests are
  justified;
- architecture and module boundaries remain valid;
- focused verification passes, or failures are accurately reported;
- affected contracts, migrations, configuration, and documentation agree;
- the final report identifies behavior, files, checks, skipped checks, and
  remaining risk.

## Maintaining module guides

Treat `docs/agent-guides/<module>.md` as a cache of verified local knowledge,
not speculative design. Include only:

- current responsibilities and explicit non-responsibilities;
- real entry points and paths;
- invariants linked to implementation and tests;
- cross-module contracts and transaction boundaries;
- adapters and configuration key names, never secret values;
- focused commands known to work;
- dated decisions and known gaps that could mislead future work.

Correct stale statements when a change invalidates them. An empty guide grants
no permission to invent its contents.
