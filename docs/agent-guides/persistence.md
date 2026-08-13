# Persistence Commands

When a persistence model changes, create an EF Core migration with:

```powershell
dotnet ef migrations add <Migration_name> `
    --project src\Haggly.Infrastructure\Haggly.Infrastructure.csproj `
    --startup-project src\Haggly.Api\Haggly.Api.csproj `
    -- `
    --connection "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
```