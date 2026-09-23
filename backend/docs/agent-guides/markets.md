# Markets Testing

Markets Domain behavior belongs in `backend/tests/Haggly.UnitTests/Domain/Modules/Markets`
when an entity gains an invariant or state transition. Application command and
query behavior belongs in `backend/tests/Haggly.UnitTests/Application/Modules/Markets`.
Use real handlers and Domain objects; substitute only `IMarketCommandRepository`,
`IStallCommandRepository`, `IMarketQuery`, `IStallQuery`, clocks, or other
Application ports.

Do not unit-test a query that only performs a trivial projection with no
validation, ownership decision, defaulting, or failure translation. Market and
Stall PostgreSQL mappings and HTTP contracts remain boundary concerns.

## Public stall details

`GET /api/v1/stalls/{id}` is anonymous and returns buyer-safe details for an
active, non-deleted stall: ID, code, name, location description, and phone
number. Missing, inactive, suspended, or closed stalls return not found. Admin
stall CRUD remains under `/api/v1/markets/stalls` and retains admin-only
authorization.

```powershell
dotnet test backend\tests\Haggly.UnitTests\Haggly.UnitTests.csproj --filter "FullyQualifiedName~Markets"
```
