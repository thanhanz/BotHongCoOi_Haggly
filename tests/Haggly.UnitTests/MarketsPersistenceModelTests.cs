using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Markets;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests;

public sealed class MarketsPersistenceModelTests
{
    [Fact]
    public void MapMarketsEntities_WhenModelIsBuilt_UsesMarketsSchema()
    {
        using var context = CreateContext();

        AssertTable(context, typeof(Market), "markets");
        AssertTable(context, typeof(Stall), "stalls");
    }

    [Fact]
    public void ConfigureMarketsEntities_WhenModelIsBuilt_UsesSoftDeleteFiltersAndActiveCodeIndexes()
    {
        using var context = CreateContext();

        var market = context.Model.FindEntityType(typeof(Market))!;
        var stall = context.Model.FindEntityType(typeof(Stall))!;

        Assert.NotEmpty(market.GetDeclaredQueryFilters());
        Assert.NotEmpty(stall.GetDeclaredQueryFilters());
        Assert.Contains(market.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Market.Code)]));
        Assert.Contains(stall.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Stall.MarketId), nameof(Stall.Code)]));
    }

    [Fact]
    public void ConfigureStallRelationships_WhenModelIsBuilt_UsesRestrictDeleteBehavior()
    {
        using var context = CreateContext();

        var stall = context.Model.FindEntityType(typeof(Stall))!;
        var marketForeignKey = stall.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Market));
        var vendorForeignKey = stall.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(VendorProfile));

        Assert.Equal(DeleteBehavior.Restrict, marketForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, vendorForeignKey.DeleteBehavior);
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }

    private static void AssertTable(DbContext context, Type entityType, string tableName)
    {
        var metadata = context.Model.FindEntityType(entityType)!;

        Assert.Equal(tableName, metadata.GetTableName());
        Assert.Equal("markets", metadata.GetSchema());
    }
}
