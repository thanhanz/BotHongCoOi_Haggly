# Styling and Accessibility Guide

## Styling

Use the existing Tailwind CSS setup and semantic custom properties rather than
introducing isolated color, spacing, radius, or shadow systems. Keep styles
local to the component unless they define an application-wide token or global
base behavior.

Design responsive behavior from the component's actual containers and route
composition. Do not add breakpoints or alternate layouts without a concrete
content or interaction need. Preserve the current Vietnamese-first typography
and document language unless the request changes localization scope.

## Accessibility

- Use semantic HTML before ARIA.
- Give form controls programmatic labels and associate helper/error text.
- Keep keyboard focus visible and interaction available without a pointer.
- Preserve disabled semantics instead of only changing appearance.
- Give dialogs correct focus, dismissal, title, and description behavior.
- Provide useful image alternatives, or empty alternatives for decorative
  images.
- Do not communicate status using color alone.
- Ensure loading, empty, error, and permission states remain understandable.

Ask for clarification when a requested interaction has materially different
desktop/mobile behavior or accessibility expectations that current patterns do
not establish. The user performs final interactive and visual review.

