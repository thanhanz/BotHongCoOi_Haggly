using Dapper;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Queries.Catalog;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Queries.Catalog;

public sealed class DapperCategoryQueryTests
{
    private readonly DapperDbContext dbContext;
    private readonly DapperCategoryQuery sut;

    public DapperCategoryQueryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HagglyDatabase"] =
                    "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
            })
            .Build();

        dbContext = new DapperDbContext(configuration);
        sut = new DapperCategoryQuery(dbContext);
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenCategoriesIncludeInactiveAndDeleted_ReturnsActiveCategoriesInDisplayOrder()
    {
        var first = new Category { Name = "Category A", Slug = $"category-a-{Guid.NewGuid():N}", DisplayOrder = 1 };
        var second = new Category { Name = "Category B", Slug = $"category-b-{Guid.NewGuid():N}", DisplayOrder = 2 };
        var inactive = new Category
        {
            Name = "Inactive Category",
            Slug = $"inactive-{Guid.NewGuid():N}",
            Status = CatalogStatus.INACTIVE
        };
        var deleted = new Category
        {
            Name = "Deleted Category",
            Slug = $"deleted-{Guid.NewGuid():N}",
            DeletedAt = DateTimeOffset.UtcNow
        };

        await SeedAsync(first);
        await SeedAsync(second);
        await SeedAsync(inactive);
        await SeedAsync(deleted);

        var result = await sut.GetPageAsync(new CategoryListFilter(1, 100), CancellationToken.None);
        var categories = result.Items;

        Assert.Contains(categories, category => category.Id == first.Id);
        Assert.Contains(categories, category => category.Id == second.Id);
        Assert.DoesNotContain(categories, category => category.Id == inactive.Id);
        Assert.DoesNotContain(categories, category => category.Id == deleted.Id);
        Assert.True(
            Array.IndexOf(categories.Select(category => category.Id).ToArray(), first.Id)
            < Array.IndexOf(categories.Select(category => category.Id).ToArray(), second.Id));
    }

    [Fact]
    public async Task GetActiveByIdAsync_WhenCategoryIsActiveAndNotDeleted_ReturnsCategory()
    {
        var category = new Category { Name = "Fruit", Slug = $"fruit-{Guid.NewGuid():N}" };
        await SeedAsync(category);

        var result = await sut.GetActiveByIdAsync(category.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal(category.Slug, result.Slug);
    }

    private async Task SeedAsync(Category category)
    {
        const string sql = """
            INSERT INTO catalog.categories
                ("Id", "ParentCategoryId", "Name", "Slug", "Description", "ImageUrl", "DisplayOrder", "Status", "CreatedAt", "DeletedAt")
            VALUES
                (@Id, @ParentCategoryId, @Name, @Slug, @Description, @ImageUrl, @DisplayOrder, @Status, @CreatedAt, @DeletedAt);
            """;

        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(
            sql,
            new
            {
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.Slug,
                category.Description,
                category.ImageUrl,
                category.DisplayOrder,
                Status = category.Status.ToString(),
                category.CreatedAt,
                category.DeletedAt
            });
    }
}
