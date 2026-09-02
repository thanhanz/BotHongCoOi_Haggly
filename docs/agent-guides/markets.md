# Markets Testing

Markets Domain behavior belongs in `tests/Haggly.UnitTests/Domain/Modules/Markets`
when an entity gains an invariant or state transition. Application command and
query behavior belongs in `tests/Haggly.UnitTests/Application/Modules/Markets`.
Use real handlers and Domain objects; substitute only `IMarketCommandRepository`,
`IStallCommandRepository`, `IMarketQuery`, `IStallQuery`, clocks, or other
Application ports.

Do not unit-test a query that only performs a trivial projection with no
validation, ownership decision, defaulting, or failure translation. Market and
Stall PostgreSQL mappings and HTTP contracts remain boundary concerns.

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --filter "FullyQualifiedName~Markets"
```
