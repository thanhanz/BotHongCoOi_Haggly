# Frontend API Client Guide

## Shared transport

`src/shared/api` owns transport behavior common to every endpoint:

- `config.ts` normalizes `NEXT_PUBLIC_API_BASE_URL` and defines `/api/v1`;
- `http-client.ts` owns the Axios instance, automatic bearer-token header, success
  envelope unwrapping, authenticated `401` signaling, and error conversion;
- `auth-session.ts` owns browser-session persistence shared by transport and the
  authentication provider;
- `contracts/` owns generic API response, pagination, and Problem Details types;
- `api-error.ts` owns the application-neutral error representation.

Do not add endpoint paths or feature DTOs here.

## Feature adapters

Endpoint-specific requests and wire contracts belong in
`src/features/<feature>/api`. Keep request field names, response field names,
nullability, and optionality aligned with the backend contract. Use the shared
request helper instead of creating feature-specific Axios instances.

Feature adapters do not accept or forward access tokens. The shared client attaches
the current session automatically. Cancellation uses the standard request signal
when applicable.

## Contract changes

For changes spanning both applications, follow the root cross-stack guide.
Inspect the backend endpoint and response types rather than inferring the
contract from a design. Account for:

- route and HTTP method;
- authorization policy;
- query, route, and body binding;
- success status and `ApiResponse<T>` data;
- validation and Problem Details responses;
- pagination metadata where applicable.

The backend owns enforcement. Frontend checks provide earlier feedback and must
not be the only protection for a business rule.
