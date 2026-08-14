using Dapper;
using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Catalog;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperProductQueryTests
{
    private readonly DapperDbContext dbContext;
    private readonly DapperProductQuery sut;

    public DapperProductQueryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HagglyDatabase"] =
                    "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
            })
            .Build();

        dbContext = new DapperDbContext(configuration);
        sut = new DapperProductQuery(dbContext);
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenProductsIncludeInactiveDeletedAndOtherCategory_ReturnsOnlyMatchingActiveProducts()
    {
        var firstCategory = await SeedActiveCategoryAsync();
        var secondCategory = await SeedActiveCategoryAsync();
        var active = new Product
        {
            CategoryId = firstCategory.Id,
            Name = $"Apple-{Guid.NewGuid():N}",
            DefaultUnit = ProductUnit.KG
        };
        var inactive = new Product
        {
            CategoryId = firstCategory.Id,
            Name = $"Inactive-{Guid.NewGuid():N}",
            DefaultUnit = ProductUnit.PIECE,
            Status = CatalogStatus.INACTIVE
        };
        var deleted = new Product
        {
            CategoryId = firstCategory.Id,
            Name = $"Deleted-{Guid.NewGuid():N}",
            DefaultUnit = ProductUnit.PIECE,
            DeletedAt = DateTimeOffset.UtcNow
        };
        var otherCategory = new Product
        {
            CategoryId = secondCategory.Id,
            Name = $"Other-{Guid.NewGuid():N}",
            DefaultUnit = ProductUnit.PIECE
        };

        await SeedProductAsync(active);
        await SeedProductAsync(inactive);
        await SeedProductAsync(deleted);
        await SeedProductAsync(otherCategory);

        var products = await sut.GetAllActiveAsync(firstCategory.Id, CancellationToken.None);

        Assert.Contains(products, product => product.Id == active.Id);
        Assert.DoesNotContain(products, product => product.Id == inactive.Id);
        Assert.DoesNotContain(products, product => product.Id == deleted.Id);
        Assert.DoesNotContain(products, product => product.Id == otherCategory.Id);
    }

    [Fact]
    public async Task GetActiveByIdAsync_WhenProductIsActiveAndNotDeleted_ReturnsProduct()
    {
        var category = await SeedActiveCategoryAsync();
        var product = new Product
        {
            CategoryId = category.Id,
            Name = $"Apple-{Guid.NewGuid():N}",
            DefaultUnit = ProductUnit.KG
        };
        await SeedProductAsync(product);

        var result = await sut.GetActiveByIdAsync(product.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.CategoryId, result.CategoryId);
        Assert.Equal(ProductUnit.KG, result.DefaultUnit);
    }

    private async Task<Category> SeedActiveCategoryAsync()
    {
        var category = new Category
        {
            Name = $"Category-{Guid.NewGuid():N}",
            Slug = $"category-{Guid.NewGuid():N}"
        };
        const string sql = """
            INSERT INTO catalog.categories
                ("Id", "Name", "Slug", "DisplayOrder", "Status", "CreatedAt")
            VALUES
                (@Id, @Name, @Slug, @DisplayOrder, @Status, @CreatedAt);
            """;

        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Slug,
            category.DisplayOrder,
            Status = category.Status.ToString(),
            category.CreatedAt
        });

        return category;
    }

    private async Task SeedProductAsync(Product product)
    {
        const string sql = """
            INSERT INTO catalog.products
                ("Id", "CategoryId", "Name", "Description", "DefaultUnit", "ImageUrl", "Status", "CreatedAt", "DeletedAt")
            VALUES
                (@Id, @CategoryId, @Name, @Description, @DefaultUnit, @ImageUrl, @Status, @CreatedAt, @DeletedAt);
            """;

        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(sql, new
        {
            product.Id,
            product.CategoryId,
            product.Name,
            product.Description,
            DefaultUnit = product.DefaultUnit.ToString(),
            product.ImageUrl,
            Status = product.Status.ToString(),
            product.CreatedAt,
            product.DeletedAt
        });
    }
}
