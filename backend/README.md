# Haggly Backend

Haggly's .NET 10 modular-monolith API for Identity, Markets, Catalog, Inventory, Sales, Payments, Finance, and Discovery.

## Local setup

Prerequisites: .NET SDK 10.0.201 (or compatible patch), Docker Compose, and PowerShell.

### 1. Start infrastructure

From the repository root:

```powershell
docker compose up -d postgres rabbitmq
docker compose ps
```

Wait for PostgreSQL and RabbitMQ to report `healthy`. Development settings use PostgreSQL at `localhost:5433` and RabbitMQ at `localhost:5672`.

### 2. Restore and build

From `backend/`:

```powershell
dotnet restore Haggly.slnx
dotnet build Haggly.slnx --no-restore
```

### 3. Install the administration tool

From `backend/`, install the repository-local tool:

```powershell
.\scripts\install-haggly-tool.ps1
```

On Linux or macOS, use `bash scripts/install-haggly-tool.sh`. The generated
tool package is kept under the ignored `.artifacts/` directory.

For local Development, the tool automatically reads
src/Haggly.Api/appsettings.Development.json; no connection setup is required.
To override it for another machine or deployment, set the connection for the
current shell instead of placing its password in command history:

```powershell
$env:HAGGLY_CONNECTION_STRING = "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
```

On Linux or macOS:

```bash
export HAGGLY_CONNECTION_STRING='Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234'
```

### 4. Apply migrations

```powershell
dotnet haggly migrate
```

The API also applies pending migrations during Development startup. The explicit command is needed before the standalone Discovery import.

### 5. Import Discovery seed data

Validate the JSON files without changing the database:

```powershell
dotnet haggly validate-reference
```

Import canonical ingredients, common dishes, and their relations:

```powershell
dotnet haggly seed-reference
```

The repeatable importer reads `seed/` by default. It also accepts
`HAGGLY_CONNECTION_STRING`, `--connection <connection-string>`, and
`--seed-dir <path>`. Migration and reference seeding are intentionally separate
commands.

Run this import before the first API start. Discovery marketplace products are seeded only after all required canonical ingredients exist. If the API was already started, import the data and restart it.

### 6. Run the API and application seed

```powershell
dotnet run --project src\Haggly.Api\Haggly.Api.csproj
```

The launch profile sets `ASPNETCORE_ENVIRONMENT=Development`. Startup creates repeatable development data for users, roles, markets, stalls, catalog, inventory, sales, and Discovery marketplace listings. Seeding does not run outside Development.

- API: `https://localhost:58557` or `http://localhost:58558`
- Swagger: `https://localhost:58557/swagger`
- RabbitMQ management: `http://localhost:15672`

### 7. Run tests

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --no-build
```

## Structure

```text
backend/
|-- src/
|-- tests/
|-- tools/
|-- seed/
|-- database/
|-- docs/agent-guides/
|-- AGENTS.md
|-- ARCHITECTURE.md
`-- Haggly.slnx
```

See `AGENTS.md` for routing and implementation policy and `ARCHITECTURE.md` for current backend boundaries. Local credentials are development-only; do not commit production secrets.
