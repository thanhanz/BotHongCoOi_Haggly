using Dapper;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Inventory;
using Haggly.IntegrationTests.Infrastructure.Persistence;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Inventory;

public sealed class DapperProductListingRepositoryTests
{
    private readonly DapperDbContext dbContext = new(IntegrationTestDatabase.CreateConfiguration());

    [Fact]
    public async Task GetPageAsync_FiltersListings_ReturnsOnlyActiveAvailableMatches()
    {
        // Arrange
        var eligible = await InventoryIntegrationScenarioFactory.CreateAsync();
        var reservedOut = await InventoryIntegrationScenarioFactory.CreateAsync();
        var inactiveProduct = await InventoryIntegrationScenarioFactory.CreateAsync();
        await InsertInventoryItemAsync(eligible, 8m, 2m);
        await ExecuteAsync(
            """
            UPDATE catalog.products
            SET "CategoryId" = @CategoryId
            WHERE "Id" IN (@ReservedProductId, @InactiveProductId);
            UPDATE catalog.product_stalls
            SET "StallId" = @StallId
            WHERE "Id" IN (@ReservedProductStallId, @InactiveProductStallId);
            UPDATE catalog.products
            SET "Status" = 'INACTIVE'
            WHERE "Id" = @InactiveProductId;
            """,
            new
            {
                eligible.CategoryId,
                eligible.StallId,
                ReservedProductId = reservedOut.ProductId,
                InactiveProductId = inactiveProduct.ProductId,
                ReservedProductStallId = reservedOut.ProductStallId,
                InactiveProductStallId = inactiveProduct.ProductStallId
            });
        await InsertInventoryItemAsync(
            eligible.InventoryId, reservedOut.ProductStallId, eligible.OwnerId, 5m, 5m);
        await InsertInventoryItemAsync(
            eligible.InventoryId, inactiveProduct.ProductStallId, eligible.OwnerId, 7m, 0m);
        var sut = new DapperProductListingRepository(dbContext);

        // Act
        var result = await sut.GetPageAsync(
            new ProductListingListFilter(eligible.CategoryId, eligible.StallId, "home", 1, 10),
            CancellationToken.None);

        // Assert
        var item = Assert.Single(result.Items);
        Assert.Equal(eligible.ProductId, item.ProductId);
        Assert.Equal(6m, item.AvailableQuantity);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_EligibleListing_ReturnsExpectedProjectionAndPagination()
    {
        // Arrange
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var inventoryItemId = await InsertInventoryItemAsync(scenario, 12.5m, 2.5m);
        await ExecuteAsync(
            """
            UPDATE catalog.products
            SET "Name" = 'Green Apple', "ImageUrl" = 'https://example.test/apple.png'
            WHERE "Id" = @ProductId;
            UPDATE catalog.product_stalls
            SET "DisplayName" = 'Fresh Apple', "SellingUnit" = 'KG',
                "MinimumOrderQuantity" = 0.500, "CurrentUnitPrice" = 42.50,
                "IsNegotiable" = TRUE
            WHERE "Id" = @ProductStallId;
            UPDATE markets.stalls
            SET "Name" = 'Orchard Stall', "Code" = @StallCode
            WHERE "Id" = @StallId;
            """,
            new
            {
                scenario.ProductId,
                scenario.ProductStallId,
                scenario.StallId,
                StallCode = $"orchard-{scenario.StallId:N}"
            });
        var sut = new DapperProductListingRepository(dbContext);

        // Act
        var result = await sut.GetPageAsync(
            new ProductListingListFilter(scenario.CategoryId, scenario.StallId, "home", 1, 1),
            CancellationToken.None);

        // Assert
        var item = Assert.Single(result.Items);
        Assert.Equal(scenario.ProductId, item.ProductId);
        Assert.Equal(scenario.ProductStallId, item.ProductStallId);
        Assert.Equal(inventoryItemId, item.InventoryItemId);
        Assert.Equal("Green Apple", item.ProductName);
        Assert.Equal("Fresh Apple", item.DisplayName);
        Assert.Equal("https://example.test/apple.png", item.ImageUrl);
        Assert.Equal(scenario.StallId, item.StallId);
        Assert.Equal("Orchard Stall", item.StallName);
        Assert.Equal($"orchard-{scenario.StallId:N}", item.StallCode);
        Assert.Equal(42.50m, item.CurrentUnitPrice);
        Assert.Equal(ProductUnit.KG, item.SellingUnit);
        Assert.Equal(0.500m, item.MinimumOrderQuantity);
        Assert.Equal(10m, item.AvailableQuantity);
        Assert.True(item.IsNegotiable);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.PageSize);
    }

    private async Task<Guid> InsertInventoryItemAsync(
        InventoryIntegrationScenario scenario,
        decimal currentQuantity,
        decimal reservedQuantity)
        => await InsertInventoryItemAsync(
            scenario.InventoryId, scenario.ProductStallId, scenario.OwnerId, currentQuantity, reservedQuantity);

    private async Task<Guid> InsertInventoryItemAsync(
        Guid inventoryId,
        Guid productStallId,
        Guid ownerId,
        decimal currentQuantity,
        decimal reservedQuantity)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO inventory.inventory_items
                ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity",
                 "Version", "CreatedAt", "CreatedBy")
            VALUES
                (@Id, @InventoryId, @ProductStallId, @CurrentQuantity, @ReservedQuantity,
                 0, @CreatedAt, @OwnerId);
            """,
            new
            {
                Id = id,
                InventoryId = inventoryId,
                ProductStallId = productStallId,
                CurrentQuantity = currentQuantity,
                ReservedQuantity = reservedQuantity,
                CreatedAt = DateTimeOffset.UtcNow,
                OwnerId = ownerId
            });
        return id;
    }

    private async Task ExecuteAsync(string sql, object parameters)
    {
        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(sql, parameters);
    }
}
