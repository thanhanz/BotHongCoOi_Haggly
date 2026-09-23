using Dapper;
using Haggly.Application.Abstractions.Sales;

namespace Haggly.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperOrderCatalog(DapperDbContext db) : IOrderCatalog
{
    public async Task<IReadOnlyList<OrderLineSnapshot>> GetOrderLinesAsync(
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken)
    {
        if (inventoryItemIds.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT inventoryItem."Id" AS "InventoryItemId",
                   inventory."StallId" AS "StallId",
                   COALESCE(NULLIF(productStall."DisplayName", ''), product."Name") AS "ProductName",
                   productStall."SellingUnit" AS "SellingUnit",
                   productStall."CurrentUnitPrice" AS "UnitPrice",
                   inventoryItem."CurrentQuantity" - inventoryItem."ReservedQuantity" AS "AvailableQuantity"
            FROM inventory.inventory_items inventoryItem
            
            INNER JOIN inventory.inventories inventory
                ON inventory."Id" = inventoryItem."InventoryId"
            INNER JOIN catalog.product_stalls productStall
                ON productStall."Id" = inventoryItem."ProductStallId"
            INNER JOIN catalog.products product
                ON product."Id" = productStall."ProductId"
            INNER JOIN markets.stalls stall
                ON stall."Id" = inventory."StallId"
            
            WHERE inventoryItem."Id" = ANY(@InventoryItemIds)
              AND inventoryItem."CurrentQuantity" - inventoryItem."ReservedQuantity" > 0
              AND productStall."DeletedAt" IS NULL
              AND productStall."IsActive" = TRUE
              AND product."DeletedAt" IS NULL
              AND product."Status" = 'ACTIVE'
              AND stall."DeletedAt" IS NULL
              AND stall."Status" = 'ACTIVE';
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<OrderLineSnapshot>(new CommandDefinition(
            sql,
            new { InventoryItemIds = inventoryItemIds.ToArray() },
            cancellationToken: cancellationToken));
        return rows.AsList();
    }
}
