# Haggly Shared Engineering Harness

This is the application-neutral operating protocol referenced by the root,
backend, and frontend `AGENTS.md` files. Side-specific implementation and
verification policies remain in the owning application directory.

## Grounding contract

Before editing:

1. Read the root router and the owning side's `AGENTS.md`.
2. Run `git status --short` and preserve the ownership baseline.
3. Determine request type, owning boundary, governing requirement, and affected
   files or layers.
4. Use `rg --files` and targeted `rg` searches to confirm paths, symbols,
   callers, contracts, and configured checks exist.
5. Read the affected implementation and one nearby working precedent.
6. Define the smallest observable result that satisfies the request.

Evidence rules:

- Proposed trees and future-state sections are direction, not implemented fact.
- Empty guides and missing paths provide no evidence.
- Executable behavior outranks descriptive documentation unless the user
  explicitly changes that behavior.
- General framework habits do not establish a Haggly convention.
- Never invent files, commands, output, test results, or runtime behavior.
- Expose material conflicts. Ask only when the choice significantly changes
  behavior or a public contract.

## Request modes

| Mode | Operating rule |
|---|---|
| Explain, review, diagnose | Inspect and report; do not modify unless asked. |
| Bug fix | Find the failing path and owning rule, then apply the side-specific regression and verification policy. |
| New behavior | Implement the smallest complete slice within the owning boundary. |
| Refactor | Preserve observable behavior and avoid unrelated feature work. |
| Contract change | Inspect producers and consumers; keep public shapes and failure behavior synchronized. |
| Architecture change | Read the root and affected side architecture completely; verify references and boundaries. |
| Documentation | Verify statements against current code/configuration and label future plans. |

## Implementation record

Before a non-trivial edit, be able to identify:

```text
Request type:
Owning application and concern:
Governing requirement or rule:
Observed implementation path:
Nearest precedent:
Checks to run:
Open assumptions or conflicts:
```

Keep this in working notes unless it helps review a consequential decision.

## Editing rules

- Keep the diff within the smallest valid ownership boundary.
- Follow a local pattern only after locating it in current code.
- Do not add unrelated cleanup, formatting, dependencies, abstractions, or
  generated artifacts.
- Do not expose secrets or edit generated files directly.
- Preserve public contracts unless the request changes them.
- Update directly affected configuration and documentation when behavior makes
  them stale.
- Update an agent guide only with durable, verified local knowledge.

## Repository artifact hygiene

Create only task-required production source, tests required by the owning
policy, migrations or generated source, configuration, and directly affected
documentation. Do not create repository-local working-note, log, report,
coverage, cache, download, SDK-home, package-cache, or scratch paths.

Build tools may update their standard ignored output, such as backend `bin/` and
`obj/` or frontend `.next/` and TypeScript build information. Do not change
ignore rules merely to conceal artifacts.

Before finishing:

1. Run `git status --short` and account for every path.
2. Remove only artifacts created by the current task.
3. Never delete or clean pre-existing modified, ignored, or untracked user data.
4. Report changes by application boundary and artifact category.

## Verification protocol

Discover checks from current manifests, project files, scripts, and CI. Use the
owning side's verification ladder. Run checks sequentially by default so the
first failure has a clear cause. Never transform unavailable, skipped, or
undiscovered checks into a passing result.

When a check fails, read its output and relevant implementation, classify the
cause, and fix the root problem within scope. Do not loosen assertions, disable
checks, or add arbitrary delays merely to obtain a passing command.

## Definition of done

Work is complete when the requested observable result is present, boundaries
and contracts remain consistent, the side-specific verification policy has
been followed, affected documentation agrees with implementation, and the
final report identifies exact checks, skipped checks, assumptions, and risks.
