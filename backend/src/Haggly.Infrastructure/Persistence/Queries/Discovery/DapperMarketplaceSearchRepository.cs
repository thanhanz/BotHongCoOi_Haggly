using Dapper;
using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Common;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Application.Modules.Discovery.Queries;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Infrastructure.Persistence.Queries.Discovery;

public sealed class DapperMarketplaceSearchRepository(DapperDbContext dbContext)
    : IMarketplaceSearchReader
{
    public async Task<MarketplaceSearchResult> SearchAsync(
        MarketplaceSearchFilter filter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM markets.stalls stall
            WHERE stall."Status" = 'ACTIVE'
              AND stall."DeletedAt" IS NULL
              AND public.haggly_normalize_search_text(stall."Name") LIKE @Contains;

            SELECT
                stall."Id",
                stall."Code",
                stall."Name",
                stall."LocationDescription",
                stall."PhoneNumber",
                (
                    SELECT COUNT(*)
                    FROM catalog.product_stalls product_stall
                    INNER JOIN catalog.products product
                        ON product."Id" = product_stall."ProductId"
                    INNER JOIN inventory.inventories inventory
                        ON inventory."StallId" = stall."Id"
                    INNER JOIN inventory.inventory_items inventory_item
                        ON inventory_item."InventoryId" = inventory."Id"
                       AND inventory_item."ProductStallId" = product_stall."Id"
                    WHERE product_stall."StallId" = stall."Id"
                      AND product."Status" = 'ACTIVE'
                      AND product."DeletedAt" IS NULL
                      AND product_stall."IsActive" = TRUE
                      AND product_stall."DeletedAt" IS NULL
                      AND inventory_item."CurrentQuantity" - inventory_item."ReservedQuantity" > 0
                ) AS "AvailableProductCount"
            FROM markets.stalls stall
            WHERE stall."Status" = 'ACTIVE'
              AND stall."DeletedAt" IS NULL
              AND public.haggly_normalize_search_text(stall."Name") LIKE @Contains
            ORDER BY
                CASE
                    WHEN public.haggly_normalize_search_text(stall."Name") = @NormalizedQuery THEN 0
                    WHEN public.haggly_normalize_search_text(stall."Name") LIKE @Prefix THEN 1
                    ELSE 2
                END,
                LENGTH(public.haggly_normalize_search_text(stall."Name")),
                stall."Name",
                stall."Id"
            OFFSET @StallOffset ROWS FETCH NEXT @StallPageSize ROWS ONLY;

            WITH paged_stalls AS (
                SELECT stall."Id"
                FROM markets.stalls stall
                WHERE stall."Status" = 'ACTIVE'
                  AND stall."DeletedAt" IS NULL
                  AND public.haggly_normalize_search_text(stall."Name") LIKE @Contains
                ORDER BY
                    CASE
                        WHEN public.haggly_normalize_search_text(stall."Name") = @NormalizedQuery THEN 0
                        WHEN public.haggly_normalize_search_text(stall."Name") LIKE @Prefix THEN 1
                        ELSE 2
                    END,
                    LENGTH(public.haggly_normalize_search_text(stall."Name")),
                    stall."Name",
                    stall."Id"
                OFFSET @StallOffset ROWS FETCH NEXT @StallPageSize ROWS ONLY
            ),
            ranked_previews AS (
                SELECT
                    stall."Id" AS "PreviewStallId",
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
                    product_stall."IsNegotiable" AS "IsNegotiable",
                    ROW_NUMBER() OVER (
                        PARTITION BY stall."Id"
                        ORDER BY
                            public.haggly_normalize_search_text(COALESCE(product_stall."DisplayName", product."Name")),
                            product_stall."Id"
                    ) AS preview_rank
                FROM paged_stalls
                INNER JOIN markets.stalls stall
                    ON stall."Id" = paged_stalls."Id"
                INNER JOIN catalog.product_stalls product_stall
                    ON product_stall."StallId" = stall."Id"
                INNER JOIN catalog.products product
                    ON product."Id" = product_stall."ProductId"
                INNER JOIN inventory.inventories inventory
                    ON inventory."StallId" = stall."Id"
                INNER JOIN inventory.inventory_items inventory_item
                    ON inventory_item."InventoryId" = inventory."Id"
                   AND inventory_item."ProductStallId" = product_stall."Id"
                WHERE public.haggly_normalize_search_text(stall."Name") = @NormalizedQuery
                  AND product."Status" = 'ACTIVE'
                  AND product."DeletedAt" IS NULL
                  AND product_stall."IsActive" = TRUE
                  AND product_stall."DeletedAt" IS NULL
                  AND inventory_item."CurrentQuantity" - inventory_item."ReservedQuantity" > 0
            )
            SELECT *
            FROM ranked_previews
            WHERE preview_rank <= 8
            ORDER BY "PreviewStallId", preview_rank;

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
              AND (
                  public.haggly_normalize_search_text(product."Name") LIKE @Contains
                  OR public.haggly_normalize_search_text(product_stall."DisplayName") LIKE @Contains
              );

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
              AND (
                  public.haggly_normalize_search_text(product."Name") LIKE @Contains
                  OR public.haggly_normalize_search_text(product_stall."DisplayName") LIKE @Contains
              )
            ORDER BY
                CASE
                    WHEN public.haggly_normalize_search_text(product."Name") = @NormalizedQuery
                      OR public.haggly_normalize_search_text(product_stall."DisplayName") = @NormalizedQuery THEN 0
                    WHEN public.haggly_normalize_search_text(product."Name") LIKE @Prefix
                      OR public.haggly_normalize_search_text(product_stall."DisplayName") LIKE @Prefix THEN 1
                    ELSE 2
                END,
                LENGTH(public.haggly_normalize_search_text(COALESCE(product_stall."DisplayName", product."Name"))),
                product."Name",
                stall."Name",
                product_stall."Id"
            OFFSET @ProductOffset ROWS FETCH NEXT @ProductPageSize ROWS ONLY;
            """;

        var parameters = new
        {
            filter.NormalizedQuery,
            Prefix = filter.NormalizedQuery + "%",
            Contains = "%" + filter.NormalizedQuery + "%",
            StallOffset = (filter.StallPage - 1) * filter.StallPageSize,
            filter.StallPageSize,
            ProductOffset = (filter.ProductPage - 1) * filter.ProductPageSize,
            filter.ProductPageSize
        };

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var stallCount = checked((int)await results.ReadSingleAsync<long>());
        var stallRows = (await results.ReadAsync<StallRow>()).AsList();
        var previewRows = (await results.ReadAsync<ProductListingRow>()).AsList();
        var productCount = checked((int)await results.ReadSingleAsync<long>());
        var productRows = (await results.ReadAsync<ProductListingRow>()).AsList();
        var products = productRows.Select(ToProductListing).ToArray();

        var previewsByStall = previewRows
            .GroupBy(row => row.PreviewStallId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<ProductListingDto>)group.Select(ToProductListing).ToArray());

        var stalls = stallRows.Select(row => new StallSearchResult(
            row.Id,
            row.Code,
            row.Name,
            row.LocationDescription,
            row.PhoneNumber,
            checked((int)row.AvailableProductCount),
            previewsByStall.GetValueOrDefault(row.Id, []))).ToArray();

        return new MarketplaceSearchResult(
            new PagedResult<StallSearchResult>(
                stalls,
                filter.StallPage,
                filter.StallPageSize,
                stallCount),
            new PagedResult<ProductListingDto>(
                products,
                filter.ProductPage,
                filter.ProductPageSize,
                productCount));
    }

    private static ProductListingDto ToProductListing(ProductListingRow row)
        => new(
            row.ProductId,
            row.ProductStallId,
            row.InventoryItemId,
            row.ProductName,
            row.DisplayName,
            row.ImageUrl,
            row.StallId,
            row.StallName,
            row.StallCode,
            row.CurrentUnitPrice,
            Enum.Parse<ProductUnit>(row.SellingUnit),
            row.MinimumOrderQuantity,
            row.AvailableQuantity,
            row.IsNegotiable);

    private sealed record StallRow(
        Guid Id,
        string Code,
        string Name,
        string? LocationDescription,
        string? PhoneNumber,
        long AvailableProductCount);

    private sealed class ProductListingRow
    {
        public Guid PreviewStallId { get; set; }
        public Guid ProductId { get; set; }
        public Guid ProductStallId { get; set; }
        public Guid InventoryItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? ImageUrl { get; set; }
        public Guid StallId { get; set; }
        public string StallName { get; set; } = string.Empty;
        public string StallCode { get; set; } = string.Empty;
        public decimal CurrentUnitPrice { get; set; }
        public string SellingUnit { get; set; } = string.Empty;
        public decimal MinimumOrderQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public bool IsNegotiable { get; set; }
    }
}
