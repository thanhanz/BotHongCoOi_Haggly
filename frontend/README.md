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
