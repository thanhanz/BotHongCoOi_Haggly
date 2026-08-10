# Haggly API Guide

## Verified conventions

- Public HTTP routes use a URL version and module prefix: `/api/v{version}/{module}`.
- `src/Haggly.Api/Endpoints/ApiRoutes.cs` owns the shared API version prefix.
- Each module owns its remaining route constants; Identity uses
  `src/Haggly.Api/Endpoints/Identity/IdentityRoutes.cs`.
- Successful Identity responses use `ApiResponse<T>`. Failures continue to use
  ASP.NET Core Problem Details.
- `AddApiServices` registers authorization policies, centralized exception
  handling, authentication failure responses, and Swashbuckle Swagger
  generation.
- In Development, the Swagger UI is available at `/swagger`, the root path
  redirects there, and the generated v1 document is available at
  `/swagger/v1/swagger.json` with JWT bearer authentication support.
- Identity Application exceptions are translated to Problem Details by
  `ApiExceptionHandler`; endpoints do not catch and translate them individually.
