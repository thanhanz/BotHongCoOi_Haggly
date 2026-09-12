# Haggly Frontend Architecture

This document describes the frontend architecture currently present under
`frontend/`. Planned choices must be labeled and must not be treated as
implemented behavior.

## Current stack

- Next.js `16.3.4` with the App Router and React Strict Mode.
- React and React DOM `19.2.8`.
- TypeScript `6.0.3` in strict, no-emit mode with the `@/* -> src/*` alias.
- Tailwind CSS `4.3.3` and semantic CSS custom properties.
- Axios `1.20.0` for HTTP transport.
- Radix UI packages for accessible behavior where currently installed.
- ESLint with Next.js Core Web Vitals and TypeScript configurations.
- Vitest and Testing Library are present for existing tests, but new frontend
  tests are opt-in under `AGENTS.md`.

Use `package.json`, the lockfile, configuration, and current imports as the
source of truth for installed technology.

## Source boundaries

```text
src/
|-- app/                         routes, layouts, metadata, providers
|-- features/                    business-facing frontend slices
|   `-- <feature>/
|       |-- api/                 endpoint paths and TypeScript contracts
|       `-- components/          feature-specific presentation
|-- shared/
|   |-- api/                     generic HTTP transport and common envelopes
|   |-- layout/                  application shell composition
|   |-- lib/                     small framework-neutral utilities
|   `-- ui/                      reusable visual primitives
`-- styles/                      semantic design tokens
```

Current feature roots are `categories`, `identity`, `product-listings`,
`products`, and `stalls`. Current routes include the home page, registration,
products, stall details, and the development-only design-system showcase.

## Dependency direction

```text
app routes -> feature modules -> shared API/UI/lib
app routes -> shared layout/UI
feature modules -> shared API/UI/lib
shared UI -> shared lib and styles
```

Shared code must not import from a feature or route. A feature should not import
another feature's internal files; expose a deliberate public entry point or
move genuinely shared behavior to `src/shared`. Avoid promoting code merely
because another possible consumer might appear later.

## Rendering and composition

App Router files own URL structure, route parameters, metadata, layouts, and
page composition. Keep pages thin enough that feature behavior and reusable
presentation remain discoverable in their owning modules. Add `"use client"`
only to the smallest boundary that needs browser state, effects, event handlers,
or client-only APIs.

The root layout sets Vietnamese document language, loads the current fonts,
wraps pages with `Providers`, and composes `BaseLayout`. `Providers` is currently
a pass-through boundary; add providers only for a concrete application-wide
dependency.

## API boundary

`src/shared/api` owns the Axios instance, base URL configuration, generic
`ApiResponse<T>`, pagination, Problem Details, and common error translation. It
must remain independent of endpoint-specific DTOs.

Each `src/features/<feature>/api` directory owns its route calls and TypeScript
request/response contracts. `NEXT_PUBLIC_API_BASE_URL` supplies the backend
origin; the current default is `http://localhost:58558`, and the shared client
appends `/api/v1`. Access tokens are passed explicitly to authenticated calls.

The backend remains authoritative for business rules and authorization.

## Design system

Semantic tokens live in `src/styles/tokens.css` and are consumed through the
global Tailwind/theme setup. Reusable primitives live in `src/shared/ui`, use
focused variants, forward appropriate native props, and expose public imports
through their local `index.ts`. Field-related primitives share current internal
styles through `src/shared/ui/_internal`.

The `/dev/design-system` route is the current visual inventory for shared
primitives. It supports development review but is not an automated acceptance
suite.

## Design source

User-authored Stitch projects are the external visual and interaction source of
truth for frontend UX/UI work. At the start of every new UX/UI task, the agent
must inspect the relevant Stitch project and screens through the connected
Stitch MCP server before editing source. Previous-session descriptions, local
code, and the design-system showcase do not replace this fresh inspection.

Stitch defines intended composition, hierarchy, spacing, typography, color,
responsive presentation, assets, and represented interaction states. The
repository architecture still defines code ownership, component boundaries,
API access, accessibility requirements, and implementation quality. Translate
the design into existing tokens and primitives where they match; do not copy
generated code blindly or weaken accessible behavior to reproduce an image.

If Stitch MCP is unavailable, unauthorized, or cannot expose the relevant
design, UX/UI implementation is blocked until the user connects it or identifies
an accessible Stitch project/screen. See `docs/agent-guides/stitch-design.md`.

## Verification boundary

Frontend delivery uses TypeScript, ESLint, and production build checks according
to `AGENTS.md`. The user owns interactive and visual acceptance. Existing tests
remain in the repository but new test work is not required unless explicitly
requested.
