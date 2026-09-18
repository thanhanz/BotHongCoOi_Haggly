using Dapper;
using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery.Dtos;

namespace Haggly.Infrastructure.Persistence.Queries.Discovery;

public sealed class DapperDiscoveryQueryRepository(DapperDbContext dbContext)
    : IDiscoveryQuery
{
    public async Task<CommonDishResult?> FindDishAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "Id" AS "DishId",
                "ExternalId",
                "Name",
                "Category"
            FROM discovery.common_dishes
            WHERE "IsActive" = TRUE
              AND "NormalizedName" = @NormalizedName;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { NormalizedName = normalizedName },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CommonDishResult>(command);
    }

    public async Task<bool> DishExistsAsync(
        Guid dishId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM discovery.common_dishes
                WHERE "Id" = @DishId
                  AND "IsActive" = TRUE
            );
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { DishId = dishId },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<IReadOnlyList<CanonicalIngredientResult>> FindCanonicalIngredientsAsync(
        string normalizedQuery,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "Id",
                "Code",
                "Name",
                "Category"
            FROM discovery.canonical_ingredients
            WHERE "IsActive" = TRUE
              AND ("NormalizedName" = @NormalizedQuery OR "NormalizedName" LIKE @Prefix)
            ORDER BY "Name", "Id"
            LIMIT 20;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                NormalizedQuery = normalizedQuery,
                Prefix = normalizedQuery + "%"
            },
            cancellationToken: cancellationToken);

        var ingredients = await connection.QueryAsync<CanonicalIngredientResult>(command);
        return ingredients.AsList();
    }

    public async Task<IReadOnlyList<ProposalSourceRow>> GetProposalRowsAsync(
        Guid dishId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                dish."Id" AS "DishId",
                dish."Name" AS "DishName",
                ingredient."Id" AS "CanonicalIngredientId",
                ingredient."Code",
                ingredient."Name" AS "IngredientName",
                product."Id" AS "ProductId",
                product."Name" AS "ProductName",
                COALESCE(product."Status" = 'ACTIVE', FALSE) AS "ProductActive",
                product_stall."Id" AS "ProductStallId",
                product_stall."DisplayName",
                product_stall."SellingUnit",
                product_stall."MinimumOrderQuantity",
                product_stall."CurrentUnitPrice",
                COALESCE(product_stall."IsActive", FALSE) AS "ListingActive",
                inventory_item."Id" AS "InventoryItemId",
                inventory_item."CurrentQuantity",
                inventory_item."ReservedQuantity",
                stall."Id" AS "StallId",
                stall."Name" AS "StallName",
                COALESCE(stall."Status" = 'ACTIVE', FALSE) AS "StallActive",
                COALESCE(market."Status" = 'ACTIVE', FALSE) AS "MarketActive"
            FROM discovery.common_dish_ingredients AS relation
            INNER JOIN discovery.common_dishes AS dish
                ON dish."Id" = relation."DishId"
            INNER JOIN discovery.canonical_ingredients AS ingredient
                ON ingredient."Id" = relation."CanonicalIngredientId"
            LEFT JOIN discovery.product_ingredient_mappings AS mapping
                ON mapping."CanonicalIngredientId" = ingredient."Id"
               AND mapping."Status" = 'APPROVED'
            LEFT JOIN catalog.products AS product
                ON product."Id" = mapping."ProductId"
               AND product."DeletedAt" IS NULL
            LEFT JOIN catalog.product_stalls AS product_stall
                ON product_stall."ProductId" = product."Id"
               AND product_stall."DeletedAt" IS NULL
            LEFT JOIN inventory.inventory_items AS inventory_item
                ON inventory_item."ProductStallId" = product_stall."Id"
            LEFT JOIN markets.stalls AS stall
                ON stall."Id" = product_stall."StallId"
               AND stall."DeletedAt" IS NULL
            LEFT JOIN markets.markets AS market
                ON market."Id" = stall."MarketId"
               AND market."DeletedAt" IS NULL
            WHERE dish."Id" = @DishId
              AND dish."IsActive" = TRUE
              AND ingredient."IsActive" = TRUE
            ORDER BY ingredient."Name", ingredient."Id", product."Id", inventory_item."Id";
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { DishId = dishId },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ProposalSourceRow>(command);
        return rows.AsList();
    }
}
