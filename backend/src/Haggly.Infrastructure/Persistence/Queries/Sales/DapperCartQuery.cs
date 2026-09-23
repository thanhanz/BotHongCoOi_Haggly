using Dapper;
using Haggly.Application.Abstractions.Sales;

namespace Haggly.Infrastructure.Persistence.Queries.Sales;

public sealed class DapperCartQuery(DapperDbContext db) : ICartQuery
{
    public async Task<CartReadModel?> GetAsync(
        Guid buyerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id" AS "CartId", "BuyerId"
            FROM sales.carts
            WHERE "BuyerId" = @BuyerId;

            SELECT cartItem."Id" AS "CartItemId",
                   cartItem."InventoryItemId",
                   cartItem."CartId",
                   cartItem."Quantity",
                   cartItem."Notes",

                   stall."Id" AS "StallId",
                   stall."MarketId",
                   stall."Code" AS "StallCode",
                   stall."Name" AS "StallName",
                   stall."LocationDescription",
                   stall."PhoneNumber",

                   product."Id" AS "ProductId",
                   product."CategoryId",
                   product."Name" AS "ProductName",
                   product."Description" AS "ProductDescription",
                   product."ImageUrl",
                   
                   productStall."Id" AS "ProductStallId",
                   productStall."DisplayName",
                   productStall."SellingUnit",
                   productStall."MinimumOrderQuantity",
                   productStall."CurrentUnitPrice",
                   productStall."IsNegotiable",
                   CASE
                       WHEN productStall."DeletedAt" IS NULL
                        AND productStall."IsActive" = TRUE
                        AND product."DeletedAt" IS NULL
                        AND product."Status" = 'ACTIVE'
                        AND stall."DeletedAt" IS NULL
                        AND stall."Status" = 'ACTIVE'
                        AND market."DeletedAt" IS NULL
                        AND market."Status" = 'ACTIVE'
                       THEN inventoryItem."CurrentQuantity" - inventoryItem."ReservedQuantity"
                       ELSE 0
                   END AS "RemainingQuantity"
            FROM sales.cart_items cartItem

            INNER JOIN inventory.inventory_items inventoryItem
                ON inventoryItem."Id" = cartItem."InventoryItemId"
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
            INNER JOIN sales.carts cart
                ON cart."Id" = cartItem."CartId"

            WHERE cart."BuyerId" = @BuyerId
            ORDER BY stall."Id", cartItem."Id";
            """;

        await using var connection = await db.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new { BuyerId = buyerId },
            cancellationToken: cancellationToken));

        var header = await results.ReadSingleOrDefaultAsync<CartHeader>();
        if (header is null)
        {
            return null;
        }

        var lines = (await results.ReadAsync<CartLineRow>())
            .Select(row => new CartLineReadModel(
                row.CartItemId,
                row.InventoryItemId,
                row.ProductStallId,
                row.Quantity,
                row.Notes,
                new CartStallReadModel(
                    row.StallId,
                    row.MarketId,
                    row.StallCode,
                    row.StallName,
                    row.LocationDescription,
                    row.PhoneNumber),
                new CartProductReadModel(
                    row.ProductId,
                    row.CategoryId,
                    row.ProductName,
                    row.ProductDescription,
                    row.ImageUrl),
                new CartOfferingReadModel(
                    row.DisplayName,
                    row.SellingUnit,
                    row.MinimumOrderQuantity,
                    row.CurrentUnitPrice,
                    row.IsNegotiable),
                row.RemainingQuantity))
            .ToArray();

        return new CartReadModel(header.CartId, header.BuyerId, lines);
    }

    private sealed record CartHeader(Guid CartId, Guid BuyerId);

    private sealed record CartLineRow(
        Guid CartItemId,
        Guid InventoryItemId,
        Guid CartId,
        decimal Quantity,
        string? Notes,
        Guid StallId,
        Guid MarketId,
        string StallCode,
        string StallName,
        string? LocationDescription,
        string? PhoneNumber,
        Guid ProductId,
        Guid CategoryId,
        string ProductName,
        string? ProductDescription,
        string? ImageUrl,
        Guid ProductStallId,
        string? DisplayName,
        Haggly.Domain.Modules.Catalog.ProductUnit SellingUnit,
        decimal MinimumOrderQuantity,
        decimal CurrentUnitPrice,
        bool IsNegotiable,
        decimal RemainingQuantity);
}
