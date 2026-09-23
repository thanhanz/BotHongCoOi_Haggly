# Cross-Stack Change Guide

Use this guide only when a request explicitly spans `backend/` and `frontend/`
or changes an HTTP contract consumed by both.

## Workflow

1. Read both application `AGENTS.md` and `ARCHITECTURE.md` files.
2. Identify the backend business owner and the frontend feature consumer.
3. Record the existing route, authorization policy, request shape, success
   envelope, response data, and Problem Details behavior.
4. Decide the new contract once; do not let separate adapters invent different
   naming or optionality.
5. Implement backend enforcement and transport changes in their owning layers.
6. Update the frontend feature-local types, API call, and consuming UI states.
7. Run backend and frontend checks independently according to their guides.

## Contract ownership

The backend is authoritative for business validation, authorization, state
transitions, and persisted outcomes. The frontend owns presentation, immediate
feedback, loading and failure display, and browser interaction. Shared API
transport must remain generic; feature-specific paths and DTOs do not belong in
`frontend/src/shared/api`.

## Compatibility

Preserve an existing public contract unless the request explicitly changes it.
When a breaking change is authorized, update all in-repository consumers in the
same task. Report external consumers or deployment ordering as a remaining risk
when they cannot be verified from the repository.

