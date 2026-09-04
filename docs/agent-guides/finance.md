# Finance

## Current responsibilities

Finance owns the append-only revenue ledger and summary revenue reporting. A
completed POS sale creates one `SALE` revenue entry for its stall. A successful
online payment creates one `SALE` entry per payment allocation and stall
fulfillment. Consequently, one online order allocated across three stalls counts
as three sales in an aggregate report.

The report is a projection over `finance.revenue_ledgers`; it is not a second
source of truth. Current reports calculate:

- `TotalSales` as the number of matching `SALE` entries;
- `NetRevenue` as the sum of their `NetAmount` values;
- vendor totals grouped by the vendor's current, non-deleted stalls;
- administrator totals grouped by current, non-deleted vendors and stalls.

Refund processing, fees, gross/net separation, profit, product quantities,
detailed sale rows, inventory reports, audit logs, and export formats are not
implemented.

## Application and persistence

Report requests, responses, MediatR queries, handlers, validation, and exceptions
are under `src/Haggly.Application/Modules/Finance/Reports` and
`src/Haggly.Application/Modules/Finance/Exceptions`.
`IRevenueReportQuery` is the Application read port. Infrastructure implements it
with `DapperRevenueReportRepository`, which projects the existing Finance,
Markets, and Identity tables.

When omitted, `From` defaults to 00:00 UTC on the current UTC date, `To` defaults
to the current UTC instant, and `SaleChannel` defaults to `ALL`. Supplied times
are normalized to UTC. The period must be ordered and no longer than 366 days.
Supported channels are `ALL`, `POS`, and `ONLINE`.

Vendor identity comes from the authenticated user context. A vendor may aggregate
all owned stalls or select one owned stall. An inaccessible `stallId` is treated
as not found. Administrator reports accept optional `marketId`, `vendorId`, and
`stallId` filters; both current administrator roles have system-wide report
access because market-specific administration is not implemented.

## API

The summary-only JSON endpoints are:

```http
GET /api/v1/vendor/reports/revenue?from=&to=&saleChannel=&stallId=
GET /api/v1/admin/reports/revenue?from=&to=&saleChannel=&marketId=&vendorId=&stallId=
```

The vendor route requires `VendorOnly`; the administrator route requires
`AdminOnly`. Successful results use `ApiResponse<T>`. Report validation maps to
400 Problem Details, and an inaccessible vendor stall maps to 404. Responses do
not echo the effective filters or date range.

## Testing

Finance Domain tests prove revenue-entry construction, monetary invariants, and
idempotency keys with real Domain objects. Application tests prove payment-result
and report orchestration with real handlers and NSubstitute implementations of
Application ports. Database aggregation, uniqueness, transaction behavior, HTTP
binding, authorization, and message delivery require real-boundary coverage and
must not be simulated as Finance unit tests.

```powershell
dotnet test tests\Haggly.UnitTests\Haggly.UnitTests.csproj --filter "FullyQualifiedName~Finance"
```

The active `Haggly.FunctionalTests` project does not yet exist. The report SQL
and HTTP pipeline therefore still need PostgreSQL and API boundary coverage.
