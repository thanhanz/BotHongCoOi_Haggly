using Dapper;
using Haggly.Infrastructure.Persistence;

namespace Haggly.IntegrationTests.Infrastructure.Persistence;

internal sealed record InventoryIntegrationScenario(
    Guid OwnerId,
    Guid StallId,
    Guid ProductStallId,
    Guid ProductId);

internal static class InventoryIntegrationScenarioFactory
{
    public static async Task<InventoryIntegrationScenario> CreateAsync()
    {
        var ownerId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        var stallId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var productStallId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dbContext = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(
            """
            INSERT INTO identity.users
                ("Id", "Email", "PhoneNumber", "PasswordHash", "FullName", "Status", "CreatedAt")
            VALUES
                (@OwnerId, @Email, '', 'integration-test', 'Inventory Vendor', 'ACTIVE', @Now);

            INSERT INTO identity.vendor_profiles
                ("UserId", "BusinessName", "ApprovalStatus", "CreatedAt")
            VALUES
                (@OwnerId, 'Inventory Vendor', 'APPROVED', @Now);

            INSERT INTO markets.markets
                ("Id", "Code", "Name", "Address", "Status", "CreatedAt")
            VALUES
                (@MarketId, @MarketCode, 'Inventory Market', 'Integration Address', 'ACTIVE', @Now);

            INSERT INTO markets.stalls
                ("Id", "MarketId", "VendorId", "Code", "Name", "Status", "CreatedAt")
            VALUES
                (@StallId, @MarketId, @OwnerId, @StallCode, 'Inventory Stall', 'ACTIVE', @Now);

            INSERT INTO catalog.categories
                ("Id", "Name", "Slug", "DisplayOrder", "Status", "CreatedAt")
            VALUES
                (@CategoryId, 'Inventory Category', @CategorySlug, 0, 'ACTIVE', @Now);

            INSERT INTO catalog.products
                ("Id", "CategoryId", "Name", "DefaultUnit", "Status", "CreatedAt")
            VALUES
                (@ProductId, @CategoryId, @ProductName, 'KG', 'ACTIVE', @Now);

            INSERT INTO catalog.product_stalls
                ("Id", "StallId", "ProductId", "DisplayName", "SellingUnit",
                 "MinimumOrderQuantity", "DefaultUnitPrice", "IsNegotiable", "IsActive", "CreatedAt")
            VALUES
                (@ProductStallId, @StallId, @ProductId, 'Integration Tomato', 'KG',
                 1.000, 45.00, FALSE, TRUE, @Now);
            """,
            new
            {
                OwnerId = ownerId,
                MarketId = marketId,
                StallId = stallId,
                CategoryId = categoryId,
                ProductId = productId,
                ProductStallId = productStallId,
                Email = $"{ownerId}@integration.test",
                MarketCode = $"market-{marketId:N}",
                StallCode = $"stall-{stallId:N}",
                CategorySlug = $"category-{categoryId:N}",
                ProductName = $"Product-{productId:N}",
                Now = now
            });

        return new(ownerId, stallId, productStallId, productId);
    }
}
