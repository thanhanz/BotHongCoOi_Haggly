# Haggly API Guide

## Verified conventions

- Public HTTP routes use a URL version and module prefix: `/api/v{version}/{module}`.
- `src/Haggly.Api/Endpoints/ApiRoutes.cs` owns the shared API version prefix.
- Each module owns its remaining route constants; Identity uses
  `src/Haggly.Api/Endpoints/Identity/IdentityRoutes.cs`.
- Successful Identity responses use `ApiResponse<T>`. Failures continue to use
  ASP.NET Core Problem Details.
- `AddApiServices` registers authorization policies, centralized exception
  handling, authentication failure responses, and the OpenAPI v1 document.
- The OpenAPI document is available at `/openapi/v1.json` in Development and
  declares JWT bearer authentication for protected operations.
- Identity Application exceptions are translated to Problem Details by
  `ApiExceptionHandler`; endpoints do not catch and translate them individually.
