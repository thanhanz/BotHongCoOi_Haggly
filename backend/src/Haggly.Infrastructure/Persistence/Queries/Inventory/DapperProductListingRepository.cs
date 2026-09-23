using Dapper;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Queries;

namespace Haggly.Infrastructure.Persistence.Queries.Inventory;

public sealed class DapperProductListingRepository(DapperDbContext dbContext) : IProductListingReader
{
    public async Task<PagedResult<ProductListingDto>> GetPageAsync(
        ProductListingListFilter filter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM catalog.products product
            INNER JOIN catalog.product_stalls product_stall
                ON product_stall."ProductId" = product."Id"
            INNER JOIN markets.stalls stall
                ON stall."Id" = product_stall."StallId"
            INNER JOIN inventory.inventories inventory
                ON inventory."StallId" = stall."Id"
            INNER JOIN inventory.inventory_items inventory_item
                ON inventory_item."InventoryId" = inventory."Id"
               AND inventory_item."ProductStallId" = product_stall."Id"
            WHERE product."Status" = 'ACTIVE'
              AND product."DeletedAt" IS NULL
              AND product_stall."IsActive" = TRUE
              AND product_stall."DeletedAt" IS NULL
              AND stall."Status" = 'ACTIVE'
              AND stall."DeletedAt" IS NULL
              AND inventory_item."CurrentQuantity" - inventory_item."ReservedQuantity" > 0
              AND (@CategoryId IS NULL OR product."CategoryId" = @CategoryId)
              AND (@StallId IS NULL OR stall."Id" = @StallId);

            SELECT
                product."Id" AS "ProductId",
                product_stall."Id" AS "ProductStallId",
                inventory_item."Id" AS "InventoryItemId",
                product."Name" AS "ProductName",
                product_stall."DisplayName" AS "DisplayName",
                product."ImageUrl" AS "ImageUrl",
                stall."Id" AS "StallId",
                stall."Name" AS "StallName",
                stall."Code" AS "StallCode",
                product_stall."CurrentUnitPrice" AS "CurrentUnitPrice",
                product_stall."SellingUnit" AS "SellingUnit",
                product_stall."MinimumOrderQuantity" AS "MinimumOrderQuantity",
                inventory_item."CurrentQuantity" - inventory_item."ReservedQuantity" AS "AvailableQuantity",
                product_stall."IsNegotiable" AS "IsNegotiable"
            FROM catalog.products product
            INNER JOIN catalog.product_stalls product_stall
                ON product_stall."ProductId" = product."Id"
            INNER JOIN markets.stalls stall
                ON stall."Id" = product_stall."StallId"
            INNER JOIN inventory.inventories inventory
                ON inventory."StallId" = stall."Id"
            INNER JOIN inventory.inventory_items inventory_item
                ON inventory_item."InventoryId" = inventory."Id"
               AND inventory_item."ProductStallId" = product_stall."Id"
            WHERE product."Status" = 'ACTIVE'
              AND product."DeletedAt" IS NULL
              AND product_stall."IsActive" = TRUE
              AND product_stall."DeletedAt" IS NULL
              AND stall."Status" = 'ACTIVE'
              AND stall."DeletedAt" IS NULL
              AND inventory_item."CurrentQuantity" - inventory_item."ReservedQuantity" > 0
              AND (@CategoryId IS NULL OR product."CategoryId" = @CategoryId)
              AND (@StallId IS NULL OR stall."Id" = @StallId)
            ORDER BY inventory_item."CreatedAt" DESC, inventory_item."Id" DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, new
        {
            filter.CategoryId,
            filter.StallId,
            Offset = (filter.Page - 1) * filter.PageSize,
            filter.PageSize
        }, cancellationToken: cancellationToken));

        var totalCount = checked((int)await results.ReadSingleAsync<long>());
        var items = (await results.ReadAsync<ProductListingDto>()).AsList();
        return new PagedResult<ProductListingDto>(items, filter.Page, filter.PageSize, totalCount);
    }
}
