# Haggly System Architecture

This document describes the boundaries shared by the Haggly backend and
frontend. Detailed implementation architecture belongs in
`backend/ARCHITECTURE.md` and `frontend/ARCHITECTURE.md`.

## System shape

Haggly currently consists of:

- a Next.js App Router website under `frontend/`;
- a .NET 10 modular-monolith HTTP API under `backend/`;
- PostgreSQL for relational persistence;
- RabbitMQ for implemented asynchronous backend workflows.

The applications are developed independently and meet at the HTTP contract.
PostgreSQL and RabbitMQ are local shared infrastructure defined in
`docker-compose.yml`; the browser never connects to either service directly.

```text
Browser
  |
  | HTTP /api/v1
  v
Next.js frontend
  |
  | typed feature API adapters
  v
.NET API -> Application -> Domain
  |                         |
  v                         v
PostgreSQL               RabbitMQ
```

## Ownership boundaries

| Concern | Owner | Detail |
|---|---|---|
| Business invariants, authorization decisions, and workflow state | Backend | `backend/ARCHITECTURE.md` |
| HTTP routes, request/response shapes, success envelopes, and Problem Details | Backend contract; frontend consumer | `backend/docs/agent-guides/api.md` and `frontend/docs/agent-guides/api-client.md` |
| Pages, interactions, responsive presentation, and browser accessibility | Frontend | `frontend/ARCHITECTURE.md` |
| Database mappings, transactions, migrations, and messaging | Backend | Backend persistence and module guides |
| Compose, CI, deployment, and cross-application documentation | Repository root | Root files and `docs/agent-guides/cross-stack.md` |

The frontend may perform convenience validation for immediate feedback, but the
backend remains authoritative. The frontend must not duplicate domain state
machines, calculate authoritative money or inventory outcomes, or access
backend storage directly.

## HTTP contract

The frontend reads the API origin from `NEXT_PUBLIC_API_BASE_URL` and defaults
to `http://localhost:58558`. Its shared transport appends `/api/v1`, unwraps the
backend `ApiResponse<T>` success envelope, and translates Problem Details into
a frontend API error. Authenticated requests receive access tokens explicitly;
the shared client does not read browser storage.

Endpoint-specific request and response types live with the owning frontend
feature. Contract changes must update the backend behavior and every affected
frontend consumer in the same task when both sides are in scope.

## Application routing

- Read `backend/AGENTS.md` for backend work.
- Read `frontend/AGENTS.md` for frontend work.
- Read `docs/agent-guides/cross-stack.md` when both applications change.

The product requirements and MVP scope remain in `README.md`. Architecture
documents record current executable boundaries and clearly label proposed
future state; they do not create requirements by themselves.

## Delivery status

The current pull-request CI workflow restores, migrates, builds, tests, and
publishes the backend. Frontend typecheck, lint, and build commands exist but
are not currently CI jobs. Frontend acceptance is therefore local and manual
unless CI is explicitly expanded in a separate change.
