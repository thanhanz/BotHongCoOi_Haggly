# Haggly API Guide

## Verified conventions

- Public HTTP routes use a URL version and module prefix: `/api/v{version}/{module}`.
- `src/Haggly.Api/Endpoints/ApiRoutes.cs` owns the shared API version prefix.
- Each module owns its remaining route constants; Identity uses
  `src/Haggly.Api/Endpoints/Identity/IdentityRoutes.cs`.
- Successful Identity responses use `ApiResponse<T>`. Failures continue to use
  ASP.NET Core Problem Details.
