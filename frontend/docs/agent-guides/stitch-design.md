# Stitch Design MCP Guide

## Mandatory scope

Use the connected Stitch MCP server before editing code for any frontend task
that creates or changes:

- a page or component's visual design;
- layout, spacing, typography, color, imagery, or hierarchy;
- desktop, tablet, or mobile responsive behavior;
- navigation or interaction presentation;
- loading, empty, error, disabled, selected, expanded, or permission states;
- shared UI primitives or design tokens.

This preflight is not required for a purely non-visual API adapter, type-only
contract update, dependency maintenance, or internal refactor whose rendered
output is explicitly unchanged.

## New-session preflight

Every new UX/UI task must perform a fresh MCP inspection. Do not rely on memory,
a previous conversation summary, an earlier implementation, or screenshots
saved from another session as proof of the current Stitch design.

Before editing:

1. Use Stitch MCP discovery/read capabilities to find the user's relevant
   project and screen set.
2. If exactly one design clearly matches the requested route or component,
   select it. If several plausible designs remain, ask the user one concise
   question identifying the choices.
3. Inspect every provided viewport and the interaction/state variants relevant
   to the requested slice.
4. Record in working notes the Stitch project, screen names or identifiers,
   inspected viewports, visible states, and unresolved design gaps.
5. Compare the design with the current route, feature components, shared
   primitives, tokens, and API contract before deciding the implementation.

Do not edit UX/UI source until these steps succeed.

## What to extract

Observe the design rather than inferring from a screen title. Capture what is
available for:

- content hierarchy and component composition;
- container widths, alignment, spacing, and responsive changes;
- typography roles, color roles, borders, radii, shadows, and imagery;
- navigation, controls, affordances, and interaction states;
- loading, empty, error, validation, disabled, and permission presentation;
- reusable patterns that should map to an existing shared primitive;
- assets available through Stitch and any usage constraints exposed by it.

When exact values are not exposed, translate the observed intent through the
nearest existing semantic token instead of inventing a parallel style system.

## Source-of-truth order

For frontend UX/UI decisions, use this order:

1. the current user's explicit request and acceptance criteria;
2. the selected current Stitch design inspected through MCP;
3. executable backend contracts and existing required behavior;
4. frontend architecture, accessibility rules, tokens, and current reusable
   component contracts;
5. nearby implementation details.

Report material conflicts instead of silently choosing. Stitch governs visual
intent, but it does not authorize changing backend business rules, weakening
accessibility, breaking public API contracts, or violating frontend ownership
boundaries.

## Implementation rules

- Reuse existing semantic tokens and shared primitives when they express the
  Stitch design accurately; extend them only for a demonstrated reusable need.
- Keep page-specific and feature-specific presentation in its owning route or
  feature.
- Do not paste Stitch-generated code without adapting it to the repository's
  React, TypeScript, Tailwind, component, and accessibility conventions.
- Use assets retrieved or referenced through the approved Stitch workflow when
  available. Do not fabricate unavailable brand assets.
- Implement the requested screen and its represented states, not unrelated
  screens discovered in the same project.

## Unavailable or incomplete Stitch access

If the Stitch MCP server is missing, disconnected, unauthorized, or cannot read
the relevant project/screen, stop before UX/UI source edits and ask the user to
connect Stitch or identify an accessible design. State the exact failed step.
Do not silently substitute web search, generated imagery, a stale screenshot,
or visual guesswork. A user may explicitly redefine the task or provide a new
source of truth, but the agent must not assume that exception.

If Stitch is accessible but omits a state or viewport that materially changes
the public component API or layout, ask one focused scope question. For minor
gaps, use the smallest accessible and responsive extension consistent with the
observed design and disclose it.

## Completion report

For every UX/UI implementation, report:

- the Stitch project and screens inspected through MCP;
- viewports and states that were available;
- the implemented route/components and reusable primitives affected;
- any inferred behavior or missing design state left for user review;
- typecheck, lint, and build outcomes under the frontend verification policy.
