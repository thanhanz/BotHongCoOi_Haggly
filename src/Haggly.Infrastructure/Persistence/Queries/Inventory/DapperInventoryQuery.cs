using Dapper;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;

namespace Haggly.Infrastructure.Persistence.Queries.Inventory;

public sealed class DapperInventoryQuery(DapperDbContext dbContext) : IInventoryQuery
{
    public async Task<InventorySession?> GetCurrentSessionAsync(
        Guid stallId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        const string sessionSql = """
            SELECT *
            FROM inventory.inventory_sessions
            WHERE "StallId" = @StallId
              AND "BusinessDate" = CAST(@BusinessDate AS date);

            SELECT l.*
            FROM inventory.daily_product_listings l
            INNER JOIN inventory.inventory_sessions s
                ON s."Id" = l."InventorySessionId"
            WHERE s."StallId" = @StallId
              AND s."BusinessDate" = CAST(@BusinessDate AS date)
            ORDER BY l."Id";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sessionSql,
            new { StallId = stallId, BusinessDate = ToDatabaseDate(businessDate) },
            cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);
        return await ReadSessionWithListingsAsync(results);
    }

    public async Task<InventorySession?> GetPreviousSessionAsync(
        Guid stallId,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        const string sessionSql = """
            SELECT *
            FROM inventory.inventory_sessions
            WHERE "StallId" = @StallId
              AND "BusinessDate" < CAST(@BusinessDate AS date)
            ORDER BY "BusinessDate" DESC, "Id" DESC
            LIMIT 1;

            SELECT l.*
            FROM inventory.daily_product_listings l
            INNER JOIN inventory.inventory_sessions s
                ON s."Id" = l."InventorySessionId"
            WHERE s."Id" = (
                SELECT "Id"
                FROM inventory.inventory_sessions
                WHERE "StallId" = @StallId
                  AND "BusinessDate" < CAST(@BusinessDate AS date)
                ORDER BY "BusinessDate" DESC, "Id" DESC
                LIMIT 1)
            ORDER BY l."Id";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sessionSql,
            new { StallId = stallId, BusinessDate = ToDatabaseDate(businessDate) },
            cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);
        return await ReadSessionWithListingsAsync(results);
    }

    public async Task<PagedResult<InventoryLedger>> GetLedgerAsync(
        InventoryLedgerListFilter filter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM inventory.inventory_ledgers l
            INNER JOIN inventory.inventory_sessions s
                ON s."Id" = l."InventorySessionId"
            WHERE s."StallId" = @StallId
              AND (@BusinessDate IS NULL OR s."BusinessDate" = CAST(@BusinessDate AS date))
              AND (@ListingId IS NULL OR l."DailyProductListingId" = @ListingId)
              AND (@TransactionType IS NULL OR l."TransactionType" = @TransactionType);

            SELECT l.*
            FROM inventory.inventory_ledgers l
            INNER JOIN inventory.inventory_sessions s
                ON s."Id" = l."InventorySessionId"
            WHERE s."StallId" = @StallId
              AND (@BusinessDate IS NULL OR s."BusinessDate" = CAST(@BusinessDate AS date))
              AND (@ListingId IS NULL OR l."DailyProductListingId" = @ListingId)
              AND (@TransactionType IS NULL OR l."TransactionType" = @TransactionType)
            ORDER BY l."OccurredAt" DESC, l."Id" DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                filter.StallId,
                BusinessDate = filter.BusinessDate is null
                    ? (DateTime?)null
                    : ToDatabaseDate(filter.BusinessDate.Value),
                filter.ListingId,
                TransactionType = filter.TransactionType?.ToString(),
                Offset = (filter.Page - 1) * filter.PageSize,
                filter.PageSize
            },
            cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);
        var totalCount = checked((int)await results.ReadSingleAsync<long>());
        var ledgers = (await results.ReadAsync<InventoryLedger>()).AsList();

        return new PagedResult<InventoryLedger>(
            ledgers,
            filter.Page,
            filter.PageSize,
            totalCount);
    }

    private static DateTime ToDatabaseDate(DateOnly value)
        => value.ToDateTime(TimeOnly.MinValue);

    private static async Task<InventorySession?> ReadSessionWithListingsAsync(
        SqlMapper.GridReader results)
    {
        var session = await results.ReadSingleOrDefaultAsync<InventorySession>();
        if (session is null)
        {
            return null;
        }

        var listings = (await results.ReadAsync<DailyProductListing>()).AsList();
        foreach (var listing in listings)
        {
            listing.InventorySession = session;
            session.DailyProductListings.Add(listing);
        }

        return session;
    }
}
