# Haggly Repository Agent Router

## Purpose

Haggly is a monorepo with two implemented application boundaries:

- `backend/`: a .NET 10 modular-monolith API;
- `frontend/`: a Next.js App Router website.

This file routes work. Application-specific architecture, implementation rules,
and verification belong to the owning directory and must not be duplicated here.

## Mandatory routing

Apply these rules before inspecting or editing application code:

| Request wording or scope | Route | Read next |
|---|---|---|
| The user says **in backend**, names .NET/API/database behavior, or targets `backend/` | Backend only | `backend/AGENTS.md`, then `backend/ARCHITECTURE.md` and the relevant backend guide |
| The user says **in frontend**, names a page/component/browser behavior, or targets `frontend/` | Frontend only | `frontend/AGENTS.md`, then `frontend/ARCHITECTURE.md` and the relevant frontend guide |
| The user says **full stack**, **across backend and frontend**, or changes an HTTP contract consumed by both | Cross-stack | Both side-specific `AGENTS.md` files and `docs/agent-guides/cross-stack.md` |
| Compose, CI, deployment, shared requirements, or repository documentation | Repository root | This file and the affected root-owned files |

Explicit wording wins over inferred ownership. Do not edit the other application
side merely because a related implementation could be useful there. If no side
is named, infer ownership from the requested outcome and current files. Ask one
concise question only when choosing a side or public contract would materially
change the result; otherwise use the smallest reversible interpretation and
state the assumption.

## Start here

1. Run `git status --short` and preserve unrelated user changes.
2. Classify the request as explanation, diagnosis, bug, feature, refactor,
   contract, architecture, documentation, or infrastructure work.
3. Route the request using the table above.
4. Use `rg --files` and targeted `rg` searches to confirm that paths, symbols,
   callers, and checks actually exist.
5. Read the nearest implementation and governing documents before editing.
6. Follow `docs/agent-guides/engineering-harness.md` for shared evidence,
   hygiene, and completion rules.

Never treat a documented future path as implemented fact. Never invent command
output or claim an unrun check passed. When a material fact is uncertain,
distinguish observed, inferred, and proposed statements.

## Repository-level sources of truth

Use this order when sources conflict:

1. Current user request and explicit acceptance criteria.
2. Executable behavior and public contracts.
3. Current implementation, configuration, and project references.
4. `README.md` for product scope and business requirements.
5. Root `ARCHITECTURE.md` for system boundaries and contract ownership.
6. The owning side's `ARCHITECTURE.md` and agent guides.

Do not silently resolve a material conflict. Report the evidence and state
which higher-priority source guided the change.

## Cross-stack contract rule

The backend owns business behavior and the HTTP contract it exposes. The
frontend owns presentation and consumes that contract through typed,
feature-local adapters. For a contract change:

1. establish the backend route, authorization, request, success envelope, and
   Problem Details behavior;
2. update the frontend feature contract and caller;
3. keep naming and optionality aligned across the boundary;
4. verify each side using its own policy.

Neither transport nor storage owns business rules. Frontend validation may
improve usability but must not become the only enforcement of a backend rule.

## Root-owned work

Root ownership includes `README.md`, `ARCHITECTURE.md`, `docker-compose.yml`,
`.github/`, `deploy/`, shared product requirements, and cross-stack guides.
Root documentation describes integration and routing; detailed backend or
frontend implementation guidance belongs beneath that application directory.

## Completion

Before finishing, run `git status --short`, account for every changed path, and
report:

- behavior or documentation changed;
- files by application boundary;
- exact checks run and their outcomes;
- checks intentionally left to the user or CI;
- remaining assumptions or risks.
