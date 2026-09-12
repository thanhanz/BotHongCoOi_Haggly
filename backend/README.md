# Haggly Backend

This is Haggly's .NET 10 modular-monolith backend. It exposes the API for
Identity, Markets, Catalog, Inventory, Sales, Payments, and Finance.

## Local development

From the repository root, start infrastructure with:

```powershell
docker compose up -d postgres rabbitmq
```

From this directory, restore and run the backend:

```powershell
dotnet restore Haggly.slnx
dotnet build Haggly.slnx --no-restore
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-build
dotnet run --project src\Haggly.Api\Haggly.Api.csproj
```

Development Swagger is available at `/swagger`. PostgreSQL and RabbitMQ
configuration is supplied by the API development settings and local Compose
services. Do not commit production secrets.

## Structure

```text
backend/
├── src/
├── tests/
├── database/
├── docs/agent-guides/
├── AGENTS.md
├── ARCHITECTURE.md
├── Haggly.slnx
├── global.json
├── Directory.Build.props
└── Directory.Packages.props
```

Agent routing and backend implementation policy are in `AGENTS.md`; current
backend boundaries and implementation state are in `ARCHITECTURE.md`.
