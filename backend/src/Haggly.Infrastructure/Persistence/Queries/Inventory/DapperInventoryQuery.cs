using Dapper;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Infrastructure.Persistence.Queries.Inventory;

public sealed class DapperInventoryQuery(DapperDbContext dbContext) : IInventoryQuery
{
    public async Task<DomainInventory?> GetInventoryAsync(Guid stallId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT * FROM inventory.inventories WHERE "StallId" = @StallId;
            SELECT i.* FROM inventory.inventory_items i
            INNER JOIN inventory.inventories inv ON inv."Id" = i."InventoryId"
            WHERE inv."StallId" = @StallId ORDER BY i."Id";
            """;
        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { StallId = stallId }, cancellationToken: cancellationToken));
        var inventory = await results.ReadSingleOrDefaultAsync<DomainInventory>();
        if (inventory is null) return null;
        foreach (var item in await results.ReadAsync<InventoryItem>())
        {
            item.Inventory = inventory;
            inventory.Items.Add(item);
        }
        return inventory;
    }

    public async Task<InventoryItem?> GetItemAsync(Guid stallId, Guid inventoryItemId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT i.* FROM inventory.inventory_items i
            INNER JOIN inventory.inventories inv ON inv."Id" = i."InventoryId"
            WHERE inv."StallId" = @StallId AND i."Id" = @InventoryItemId;
            """;
        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<InventoryItem>(
            new CommandDefinition(sql, new { StallId = stallId, InventoryItemId = inventoryItemId },
                cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<InventoryLedger>> GetLedgerAsync(
        InventoryLedgerListFilter filter, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*) FROM inventory.inventory_ledgers l
            INNER JOIN inventory.inventories i ON i."Id" = l."InventoryId"
            WHERE i."StallId" = @StallId
              AND (@InventoryItemId IS NULL OR l."InventoryItemId" = @InventoryItemId)
              AND (@TransactionType IS NULL OR l."TransactionType" = @TransactionType);

            SELECT l.* FROM inventory.inventory_ledgers l
            INNER JOIN inventory.inventories i ON i."Id" = l."InventoryId"
            WHERE i."StallId" = @StallId
              AND (@InventoryItemId IS NULL OR l."InventoryItemId" = @InventoryItemId)
              AND (@TransactionType IS NULL OR l."TransactionType" = @TransactionType)
            ORDER BY l."OccurredAt" DESC, l."Id" DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, new
        {
            filter.StallId,
            filter.InventoryItemId,
            TransactionType = filter.TransactionType?.ToString(),
            Offset = (filter.Page - 1) * filter.PageSize,
            filter.PageSize
        }, cancellationToken: cancellationToken));
        var totalCount = checked((int)await results.ReadSingleAsync<long>());
        var ledgers = (await results.ReadAsync<InventoryLedger>()).AsList();
        return new PagedResult<InventoryLedger>(ledgers, filter.Page, filter.PageSize, totalCount);
    }
}
