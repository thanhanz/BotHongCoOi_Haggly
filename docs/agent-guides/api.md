# Haggly API Agent Guide

## Purpose and ownership

Use this guide for work in `src/Haggly.Api`: HTTP routes, request binding,
authentication and authorization at the transport boundary, response envelopes,
Problem Details, middleware, and OpenAPI metadata.

The API project exposes business capabilities; it does not own them. Before
changing an endpoint, read the guide for the owning business module and inspect
the corresponding Domain and Application behavior. Keep business validation,
authorization decisions that require business data, orchestration, and state
changes out of endpoint handlers. Do not put persistence access in the API.

`ARCHITECTURE.md` is the project overview. This file is only the verified API
implementation guide. Feature-specific route catalogs and response semantics
belong in their module guides, such as `sales.md` or `inventory.md`.

## Current entry points

- `src/Haggly.Api/Program.cs` configures the application pipeline and maps all
  endpoint modules.
- `src/Haggly.Api/ApiConfigurationExtensions.cs` registers API services,
  Problem Details, authorization, OpenAPI, and JWT challenge/forbid responses.
- `src/Haggly.Api/Endpoints/ApiRoutes.cs` owns the shared `/api/v1` prefix.
- `src/Haggly.Api/Endpoints/<Module>` contains module route constants, request
  contracts, and Minimal API endpoint extension methods.
- `src/Haggly.Api/Authorization/IdentityPolicies.cs` contains reusable policy
  names; `AuthorizationConfigurationExtensions.cs` maps those policies to roles.
- `src/Haggly.Api/Middleware/ApiExceptionHandler.cs` centrally maps Application
  exceptions to HTTP Problem Details.
- `src/Haggly.Api/Responses/ApiResponse.cs` defines the successful response
  envelope.

`Program.cs` currently registers persistence and token services, then API
services. Its request pipeline runs exception handling, authentication, and
authorization before the mapped endpoints. Swagger is enabled only in the
Development environment.

## Endpoint structure

Haggly uses ASP.NET Core Minimal APIs. For a new API surface:

1. Add or extend a module-specific route constants class. Build public routes
   from `ApiRoutes.Version1`; do not repeat the version string in handlers.
2. Add transport-only request records under the module's `Requests` directory
   when the JSON body differs from an Application command or query.
3. Map related routes in a `<Feature>EndpointExtensions` class through a
   `Map<Feature>Endpoints` extension method.
4. Create a route group with `MapGroup`, give it an OpenAPI tag with
   `WithTags`, and apply the narrowest appropriate authorization policy.
5. Bind route, query, body, authenticated-principal, and cancellation inputs in
   the endpoint handler. Translate them into an Application command or query
   and dispatch it through MediatR `ISender`.
6. Wrap successful results in `ApiResponse<T>` and use the HTTP status that
   matches the operation. A create endpoint should return `201 Created` and a
   stable resource location when the created resource has a detail route.
7. Declare successful and expected Problem Details responses with `Produces`
   and `ProducesProblem` so the generated OpenAPI contract matches behavior.
8. Register the endpoint mapper in `Program.cs`.

Endpoint handlers may perform transport mapping and harmless defaults such as
pagination defaults. They must not reproduce Domain/Application invariants,
query EF or Dapper directly, or catch known Application exceptions merely to
translate them to HTTP.

## Routes and contracts

- Public routes currently use the `/api/v1` prefix.
- Keep the remainder of each route in its module route constants class. Use
  route constraints such as `{id:guid}` where the contract requires them.
- Use request types for HTTP input and Application DTOs for output unless a
  transport-specific response is required.
- Paginated endpoints return `PagedResult<T>` inside `ApiResponse<T>`.
- Preserve existing public route, JSON, status-code, and response contracts
  unless the request explicitly authorizes a breaking change.
- Do not duplicate a complete feature contract here. Record business meaning,
  invariants, and enriched response fields in the owning module guide.

The success envelope has the shape `ApiResponse<T>(Success, Message, Data)`;
`ApiResponse<T>.Create` sets `Success` to `true`. Failures do not use this
envelope.

## Authentication and authorization

JWT bearer authentication is configured by Infrastructure and added to the API
pipeline before authorization. API authorization policies currently include:

| Policy | Allowed roles |
|---|---|
| `identity:buyer` | `BUYER` |
| `identity:vendor` | `VENDOR` |
| `identity:admin` | `MARKET_ADMIN`, `PLATFORM_ADMIN` |
| `catalog:contributor` | `VENDOR`, `MARKET_ADMIN`, `PLATFORM_ADMIN` |

Use policy constants from `IdentityPolicies`; do not embed role strings in
endpoints. Apply common protection to the route group and use route-level
authorization or `AllowAnonymous` only for intentional exceptions.

When a use case needs the current user ID, read the JWT `sub` claim, with
`ClaimTypes.NameIdentifier` as the existing fallback, and pass the ID into the
Application request. Authentication proves identity and role membership at the
HTTP boundary. Ownership checks that require stored business data remain in the
Application use case.

Missing or invalid credentials return `401` Problem Details and include the
Bearer challenge. An authenticated caller who fails a policy receives `403`
Problem Details. These responses are configured centrally; endpoint handlers
must not recreate them.

## Errors and Problem Details

Known Application exceptions are mapped by `ApiExceptionHandler`:

- validation failures -> `400 Bad Request`;
- authentication failures -> `401 Unauthorized`;
- forbidden operations -> `403 Forbidden`;
- missing resources -> `404 Not Found`;
- state or uniqueness conflicts -> `409 Conflict`;
- unrecognized exceptions -> `500 Internal Server Error` with a generic client
  message and server-side error logging.

Problem Details includes `status`, `title`, `detail`, request-path `instance`,
and `traceId`. When adding an Application exception that may cross the HTTP
boundary, add its explicit centralized mapping and document the corresponding
`ProducesProblem` metadata. Do not expose stack traces, provider messages,
connection details, or secrets.

## OpenAPI and development behavior

`AddApiServices` registers endpoint discovery and the Swagger v1 document with
a JWT bearer security scheme. In Development, `UseSwaggerDocumentation` serves:

- Swagger UI at `/swagger`;
- the document at `/swagger/v1/swagger.json`;
- a root `/` redirect to `/swagger`.

Keep endpoint tags, result types, success statuses, and expected error statuses
accurate. OpenAPI metadata is part of the public contract and should be covered
by endpoint contract tests when changed.

## Testing API changes

API behavior is a real boundary and belongs in the planned
`Haggly.FunctionalTests` project. Until that project exists, do not simulate the
HTTP boundary in unit tests or claim functional verification. Follow test-first
development for behavior changes and use the nearest real-boundary style:

- endpoint contract tests verify registered method/path combinations,
  authorization metadata, response types, and documented statuses;
- authorization pipeline tests exercise missing, invalid, expired, and
  wrong-role bearer tokens;
- API integration tests execute request binding, MediatR dispatch, response
  serialization, and exception translation through an HTTP application;
- Swagger contract tests verify the generated document and Development routes.

Application use-case tests belong in `tests/Haggly.UnitTests/Application` and prove
business behavior independently of HTTP. API tests should not become duplicate
business-rule test suites. When persistence, transactions, or providers are
part of the behavior, use the appropriate real-boundary integration tests
described by the persistence and owning-module guides.

Focused commands currently used by the repository are:

```powershell
dotnet test tests/Haggly.UnitTests/Haggly.UnitTests.csproj --filter "FullyQualifiedName~Application"
dotnet test Haggly.slnx --no-build
```

Inspect the solution, projects, and current build state before adding
`--no-build` or choosing a broader command.

## API change checklist

Before completing an API change, verify that:

- the owning module and Application use case are identified;
- the API contains transport mapping only;
- route constants, version prefix, HTTP method, and status codes are correct;
- authentication, policy, current-user propagation, and ownership boundaries
  are correct;
- success responses use `ApiResponse<T>` and failures use Problem Details;
- every expected Application exception has a centralized mapping;
- OpenAPI metadata matches runtime behavior;
- endpoint mapping is registered in `Program.cs`;
- focused contract/pipeline tests and relevant Application tests pass;
- the owning module guide is updated when the feature contract or business
  behavior changes.
