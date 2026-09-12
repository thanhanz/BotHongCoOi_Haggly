# Haggly Frontend

The Haggly website uses Next.js App Router, React, TypeScript, and Tailwind CSS.
The initial slice provides semantic design tokens, accessible shared UI
primitives, and a development-only showcase at `/dev/design-system`.

## Commands

```powershell
corepack enable
pnpm install
pnpm dev
pnpm typecheck
pnpm lint
pnpm test
pnpm build
```

The website consumes the API from `../backend`. Business rules remain in the
backend; frontend feature modules compose generic primitives from
`src/shared/ui`.

Agent routing and frontend implementation policy are in `AGENTS.md`; current
frontend boundaries are in `ARCHITECTURE.md`, with focused guides under
`docs/agent-guides/`.

## API client

Set `NEXT_PUBLIC_API_BASE_URL` to the API origin. Local development defaults to
`http://localhost:58558`, matching the backend HTTP launch profile.

Shared Axios transport, success envelopes, pagination, and Problem Details
contracts live in `src/shared/api`. Endpoint-specific requests and response
contracts remain with their owning feature under `src/features/<feature>/api`.
Access tokens are passed explicitly to authenticated API calls; the shared
client does not read browser storage.
