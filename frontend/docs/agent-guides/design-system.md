# Frontend Design System Guide

## Ownership

`src/shared/ui` contains reusable presentation primitives. Semantic tokens live
in `src/styles/tokens.css`; the current visual inventory is available at
`/dev/design-system`.

Current primitives include avatar, badge, button, card, checkbox, container,
dialog, divider, input, radio, select, textarea, and typography components.

## Rules

- Reuse existing semantic tokens for brand, surface, text, border, state,
  spacing, shape, elevation, and control sizing.
- Prefer semantic variants such as intent, size, and state over arbitrary style
  escape hatches.
- Preserve native element props, labels, focus behavior, disabled behavior, and
  keyboard interaction.
- Use Radix UI when an installed primitive solves non-trivial accessible
  behavior; do not add a dependency for simple semantic HTML.
- Share implementation-only field styles through `_internal`; do not expose
  internal modules as public component APIs.
- Export a primitive through its local `index.ts`.
- Keep product-specific text, API types, and business behavior out of shared UI.

Add a new primitive only for a current caller. If a component has only one
feature-specific use, keep it in that feature until a stable reusable contract
is demonstrated.

When a shared primitive changes, review its current callers and the design-system
showcase. Interactive and visual acceptance remains with the user.

