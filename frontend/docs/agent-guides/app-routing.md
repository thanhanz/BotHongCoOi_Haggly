# App Routing Guide

## Ownership

`src/app` owns Next.js App Router concerns: URL structure, route parameters,
layouts, metadata, providers, loading/error/not-found boundaries, and page-level
composition. It does not own reusable controls or endpoint-specific transport.

## Current entry points

- `src/app/layout.tsx`: root metadata, Vietnamese document language, fonts,
  providers, and base layout.
- `src/app/providers.tsx`: application-wide provider boundary; currently
  pass-through.
- `src/app/page.tsx`: home route.
- `src/app/register/page.tsx`: registration route.
- `src/app/products/page.tsx`: product route.
- `src/app/stalls/[stallId]/page.tsx`: parameterized stall detail route.
- `src/app/dev/design-system/page.tsx`: development visual inventory.
- `src/app/not-found.tsx`: route-level not-found presentation.

## Rules

- Keep route files focused on route data, metadata, and composition.
- Put feature-specific API calls and contracts in the owning feature.
- Put reusable presentation in `src/shared/ui` and feature-specific
  presentation in the feature's `components` directory.
- Prefer server components. Add `"use client"` only at the smallest component
  boundary that requires browser capabilities or interaction state.
- Validate and normalize route parameters before passing them to feature calls.
- Represent meaningful loading, empty, failure, forbidden, and not-found states
  when the route can encounter them.
- Do not add a global provider for state that can remain local or server-owned.

Run `pnpm build` when changes affect routes, layouts, metadata, server/client
boundaries, or production rendering.

