using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Catalog;

public sealed class CategoryPersistenceModelTests
{
    [Fact]
    public void MapCategory_WhenModelIsBuilt_UsesCatalogCategoriesTable()
    {
        using var context = CreateContext();

        var category = context.Model.FindEntityType(typeof(Category))!;

        Assert.Equal("categories", category.GetTableName());
        Assert.Equal("catalog", category.GetSchema());
    }

    [Fact]
    public void ConfigureCategory_WhenModelIsBuilt_UsesSoftDeleteFilterAndActiveSlugIndex()
    {
        using var context = CreateContext();

        var category = context.Model.FindEntityType(typeof(Category))!;
        var slugIndex = category.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Category.Slug)]));

        Assert.NotEmpty(category.GetDeclaredQueryFilters());
        Assert.True(slugIndex.IsUnique);
        Assert.Equal("\"DeletedAt\" IS NULL", slugIndex.GetFilter());
    }

    [Fact]
    public void ConfigureCategory_WhenModelIsBuilt_UsesRestrictParentRelationshipAndListIndexes()
    {
        using var context = CreateContext();

        var category = context.Model.FindEntityType(typeof(Category))!;
        var parentForeignKey = Assert.Single(category.GetForeignKeys());

        Assert.Equal(DeleteBehavior.Restrict, parentForeignKey.DeleteBehavior);
        Assert.Contains(category.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Category.ParentCategoryId)]));
        Assert.Contains(category.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Category.Status), nameof(Category.DisplayOrder), nameof(Category.Name)]));
    }

    [Fact]
    public void MapCategory_WhenModelIsBuilt_MapsProductStallAssociation()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(ProductStall)));
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }
}
