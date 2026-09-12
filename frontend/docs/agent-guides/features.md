# Frontend Feature Guide

## Ownership

`src/features/<feature>` groups the frontend code for one user-facing business
capability. A feature may own endpoint adapters, TypeScript contracts, and
presentation used by its routes. Backend business behavior remains outside the
frontend feature.

Current features are `categories`, `identity`, `product-listings`, `products`,
and `stalls`.

## Structure

Use only directories justified by current code:

```text
features/<feature>/
|-- api/
|   |-- <feature>.api.ts
|   |-- <feature>.contracts.ts
|   `-- index.ts
`-- components/
    |-- FeatureComponent.tsx
    `-- index.ts
```

Do not create empty folders or a universal feature template. Add hooks, state,
or other subdirectories only when the feature has an implemented responsibility
for them.

## Placement decisions

- Keep a component feature-local while its language, data, or behavior belongs
  to one capability.
- Move a component to `src/shared/ui` only after it has a stable generic API and
  a real cross-feature use.
- Keep API DTOs aligned with the backend wire contract; do not reshape them into
  shared domain models.
- Perform presentation formatting near the feature consumer. Do not reproduce
  authoritative backend calculations or workflow transitions.
- Export intentional public entry points through `index.ts`; avoid deep imports
  into another feature's internals.

Before designing a reusable component API, clarify real callers, required
states, responsive behavior, and expected variants when current code does not
answer them.

