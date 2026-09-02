# Persistence Commands

Persistence is not covered by `Haggly.UnitTests`; that project intentionally
references only Domain and Application. EF Core mappings, PostgreSQL constraints,
Dapper result shapes, migrations, concurrency, and transactions require a real
database boundary and belong in the planned `Haggly.FunctionalTests` project.
Until that project exists, do not claim persistence-boundary verification. Do
not mock EF Core or Dapper to move persistence coverage into unit tests.

When a persistence model changes, create an EF Core migration with:

```powershell
dotnet ef migrations add <Migration_name> `
    --project src\Haggly.Infrastructure\Haggly.Infrastructure.csproj `
    --startup-project src\Haggly.Api\Haggly.Api.csproj `
    -- `
    --connection "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
```
