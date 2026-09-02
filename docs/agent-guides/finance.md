# Finance Testing

Finance Domain tests prove revenue-entry construction, monetary invariants, and
idempotency keys with real Domain objects. Application tests prove payment-result
orchestration with real handlers and NSubstitute implementations of Application
ports. Database uniqueness, transaction behavior, and message delivery require
real-boundary coverage and must not be simulated as Finance unit tests.

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --filter "FullyQualifiedName~Finance"
```
