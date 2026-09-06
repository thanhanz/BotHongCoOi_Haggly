# Haggly Frontend

The Haggly website uses Next.js App Router, React, TypeScript, and Tailwind CSS.
The initial slice provides semantic design tokens, accessible shared UI
primitives, and a development-only showcase at `/dev/design-system`.

## Commands

```powershell
npm.cmd install
npm.cmd run dev
npm.cmd run typecheck
npm.cmd run lint
npm.cmd run test
npm.cmd run build
```

The website consumes the API from `../backend`. Business rules remain in the
backend; frontend feature modules compose generic primitives from
`src/shared/ui`.
