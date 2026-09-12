# Frontend API Client Guide

## Shared transport

`src/shared/api` owns transport behavior common to every endpoint:

- `config.ts` normalizes `NEXT_PUBLIC_API_BASE_URL` and defines `/api/v1`;
- `http-client.ts` owns the Axios instance, explicit bearer-token header, success
  envelope unwrapping, and error conversion;
- `contracts/` owns generic API response, pagination, and Problem Details types;
- `api-error.ts` owns the application-neutral error representation.

Do not add endpoint paths, feature DTOs, or browser-storage access here.

## Feature adapters

Endpoint-specific requests and wire contracts belong in
`src/features/<feature>/api`. Keep request field names, response field names,
nullability, and optionality aligned with the backend contract. Use the shared
request helper instead of creating feature-specific Axios instances.

Authenticated functions accept the access token explicitly. Cancellation uses
the standard request signal when applicable.

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

