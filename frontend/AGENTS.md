# Haggly Frontend Agent Guide

## Scope

The frontend is an implemented Next.js App Router application using React,
TypeScript, and Tailwind CSS. Frontend requests authorize implementation under
this directory; it is not a placeholder. Read the root `../AGENTS.md` first,
then use this file for frontend-only work.

## Start here

1. Read `ARCHITECTURE.md` and the relevant guide in `docs/agent-guides/`.
2. Inspect the target route, feature, shared primitive, API adapter, and nearest
   working example before editing.
3. Confirm the backend route and public contract when real API data is involved.
4. Implement the smallest usable, maintainable component or vertical UI slice.
5. Apply the shared evidence and hygiene rules in
   `../docs/agent-guides/engineering-harness.md`.

## Frontend routing

| Concern | Owner | Read next |
|---|---|---|
| Pages, layouts, route parameters, providers, metadata | App Router | `docs/agent-guides/app-routing.md` |
| Feature components and feature-local API code | Feature | `docs/agent-guides/features.md` |
| Axios transport, envelopes, Problem Details, authentication token flow | Shared API | `docs/agent-guides/api-client.md` |
| Reusable controls, tokens, and component variants | Design system | `docs/agent-guides/design-system.md` |
| Tailwind, responsive behavior, semantics, keyboard use | Styling/accessibility | `docs/agent-guides/styling-accessibility.md` |

## Implementation policy

- Compose routes in `src/app` from feature and shared modules.
- Keep endpoint-specific calls and TypeScript contracts in
  `src/features/<feature>/api`.
- Keep feature-specific presentation in `src/features/<feature>/components`.
- Promote code to `src/shared` only when it has a real cross-feature consumer or
  stable application-wide responsibility.
- Reuse `src/shared/ui` primitives and `src/styles/tokens.css` before creating
  new visual conventions.
- Keep authoritative business rules, authorization, money, inventory, and
  workflow transitions in the backend. Browser validation is user feedback,
  not enforcement.
- Prefer focused props and composition. Avoid speculative generic component
  APIs, global state, context providers, and variants without a current caller.
- Preserve backend request/response naming and optionality in feature API
  contracts.

## Clarifying component scope

Ask a concise question before editing only when an unanswered choice would
materially affect the component's public API, ownership, or future reuse. Useful
questions include:

- Is the component page-specific or intended for multiple features?
- Which loading, empty, error, disabled, and permission states are required?
- Which API contract or representative data shape should it consume?
- Which responsive layouts and interactions are required?
- Which variants are expected by a known near-term caller?
- Are localization or accessibility requirements different from the existing
  application defaults?

Do not turn routine implementation into an interview. Infer answers from nearby
code when evidence exists; otherwise choose the smallest reversible design and
state the assumption.

## Frontend test and verification policy

Do not create or expand frontend unit, component, integration, or end-to-end
tests unless the user explicitly requests tests. Do not delete or weaken the
existing tests. The user owns interactive, visual, and business-flow acceptance
after code edits.

Automated compile-quality checks still apply because the delivered code must be
usable:

1. Run `pnpm typecheck` for TypeScript changes.
2. Run `pnpm lint` for source changes.
3. Run `pnpm build` when routes, rendering boundaries, configuration, or
   production bundling are affected.

Existing `pnpm test` coverage may be run when the user requests it or when
diagnosing an existing test failure; it is not a default completion gate. If a
check cannot run, report the limitation without substituting an invented check.

## Completion

Report the pages/components/contracts changed, exact typecheck/lint/build
commands run, skipped checks, and any interactive states left for the user to
verify. Update `ARCHITECTURE.md` or a frontend guide only when the change
establishes or invalidates durable knowledge.
