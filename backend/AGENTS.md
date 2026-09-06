# Haggly Backend Agent Guide

The Haggly backend is a .NET 10 modular monolith. Backend source, tests,
migrations, and solution files live under this directory.

Read the repository-level `../AGENTS.md` first, then use the module guides in
`../docs/agent-guides/`. Backend commands are run from this directory unless a
command explicitly uses a repository-root path.

The project boundaries remain:

- `src/Haggly.Domain`: business state and invariants only.
- `src/Haggly.Application`: use cases, validation, ports, and orchestration.
- `src/Haggly.Infrastructure`: persistence, messaging, authentication, and
  provider adapters.
- `src/Haggly.Api`: HTTP transport, authorization policies, middleware, and
  OpenAPI.
- `tests`: active unit and legacy boundary tests.
- `database`: database assets owned by the backend.

Use the backend verification ladder documented in `../AGENTS.md` and
`../docs/agent-guides/engineering-harness.md`.
