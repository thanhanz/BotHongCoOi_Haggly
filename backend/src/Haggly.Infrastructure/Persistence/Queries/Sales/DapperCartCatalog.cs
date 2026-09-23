using Dapper;
using Haggly.Application.Abstractions.Sales;

namespace Haggly.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperCartCatalog(DapperDbContext db) : ICartCatalog
{
    public async Task<IReadOnlyList<CartItemSnapshot>> GetItemsAsync(
        IReadOnlyCollection<Guid> inventoryItemIds,
        CancellationToken cancellationToken)
    {
        if (inventoryItemIds.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT inventoryItem."Id" AS "InventoryItemId",
                   productStall."Id" AS "ProductStallId",
                   inventory."StallId" AS "StallId",
                   COALESCE(NULLIF(productStall."DisplayName", ''), product."Name") AS "ProductName",
                   productStall."SellingUnit" AS "SellingUnit",
                   productStall."MinimumOrderQuantity" AS "MinimumOrderQuantity",
                   productStall."CurrentUnitPrice" AS "UnitPrice",
                   productStall."IsNegotiable" AS "IsNegotiable",
                   inventoryItem."CurrentQuantity" - inventoryItem."ReservedQuantity" AS "RemainingQuantity",
                   TRUE AS "IsOrderable"
            FROM inventory.inventory_items inventoryItem

            INNER JOIN inventory.inventories inventory
                ON inventory."Id" = inventoryItem."InventoryId"
            INNER JOIN catalog.product_stalls productStall
                ON productStall."Id" = inventoryItem."ProductStallId"
            INNER JOIN catalog.products product
                ON product."Id" = productStall."ProductId"
            INNER JOIN markets.stalls stall
                ON stall."Id" = inventory."StallId"
            INNER JOIN markets.markets market
                ON market."Id" = stall."MarketId"
                
            WHERE inventoryItem."Id" = ANY(@InventoryItemIds)
              AND productStall."DeletedAt" IS NULL
              AND productStall."IsActive" = TRUE
              AND product."DeletedAt" IS NULL
              AND product."Status" = 'ACTIVE'
              AND stall."DeletedAt" IS NULL
              AND stall."Status" = 'ACTIVE'
              AND market."DeletedAt" IS NULL
              AND market."Status" = 'ACTIVE';
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<CartItemSnapshot>(new CommandDefinition(
            sql,
            new { InventoryItemIds = inventoryItemIds.ToArray() },
            cancellationToken: cancellationToken));
        return rows.AsList();
    }
}
