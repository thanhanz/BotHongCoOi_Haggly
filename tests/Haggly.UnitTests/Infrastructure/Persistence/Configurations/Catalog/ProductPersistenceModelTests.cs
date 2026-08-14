using Haggly.Domain.Modules.Catalog;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductPersistenceModelTests
{
    [Fact]
    public void MapProduct_WhenModelIsBuilt_UsesCatalogProductsTable()
    {
        using var context = CreateContext();

        var product = context.Model.FindEntityType(typeof(Product))!;

        Assert.Equal("products", product.GetTableName());
        Assert.Equal("catalog", product.GetSchema());
    }

    [Fact]
    public void ConfigureProduct_WhenModelIsBuilt_UsesSoftDeleteFilterAndActiveCategoryNameIndex()
    {
        using var context = CreateContext();

        var product = context.Model.FindEntityType(typeof(Product))!;
        var nameIndex = product.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Product.CategoryId), nameof(Product.Name)]));

        Assert.NotEmpty(product.GetDeclaredQueryFilters());
        Assert.True(nameIndex.IsUnique);
        Assert.Equal("\"DeletedAt\" IS NULL", nameIndex.GetFilter());
    }

    [Fact]
    public void ConfigureProduct_WhenModelIsBuilt_UsesRestrictCategoryRelationshipAndCategoryStatusIndex()
    {
        using var context = CreateContext();

        var product = context.Model.FindEntityType(typeof(Product))!;
        var categoryForeignKey = Assert.Single(product.GetForeignKeys());

        Assert.Equal(DeleteBehavior.Restrict, categoryForeignKey.DeleteBehavior);
        Assert.Contains(product.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Product.CategoryId), nameof(Product.Status)]));
    }

    [Fact]
    public void MapProduct_WhenModelIsBuilt_MapsProductStallAssociation()
    {
        using var context = CreateContext();

        var productStall = context.Model.FindEntityType(typeof(ProductStall))!;
        Assert.Equal("product_stalls", productStall.GetTableName());
        Assert.Equal("catalog", productStall.GetSchema());
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }
}
