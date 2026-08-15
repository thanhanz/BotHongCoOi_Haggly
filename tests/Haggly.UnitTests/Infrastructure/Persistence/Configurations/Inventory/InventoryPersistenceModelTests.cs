using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryPersistenceModelTests
{
    [Fact]
    public void MapInventoryEntities_WhenModelIsBuilt_UsesInventorySchemaAndTables()
    {
        using var context = CreateContext();

        AssertTable(context, typeof(InventorySession), "inventory_sessions");
        AssertTable(context, typeof(DailyProductListing), "daily_product_listings");
        AssertTable(context, typeof(InventoryLedger), "inventory_ledgers");
    }

    [Fact]
    public void ConfigureInventoryEntities_WhenModelIsBuilt_UsesUniqueIndexes()
    {
        using var context = CreateContext();

        var session = context.Model.FindEntityType(typeof(InventorySession))!;
        var listing = context.Model.FindEntityType(typeof(DailyProductListing))!;

        Assert.Contains(session.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(InventorySession.StallId), nameof(InventorySession.BusinessDate)]));
        Assert.Contains(listing.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([
                    nameof(DailyProductListing.InventorySessionId),
                    nameof(DailyProductListing.ProductStallId)]));
    }

    [Fact]
    public void ConfigureInventoryEntities_WhenModelIsBuilt_UsesPrecisionAndStringEnums()
    {
        using var context = CreateContext();

        var listing = context.Model.FindEntityType(typeof(DailyProductListing))!;
        var ledger = context.Model.FindEntityType(typeof(InventoryLedger))!;

        AssertPrecision(listing, nameof(DailyProductListing.PublicUnitPrice), 18, 2);
        AssertPrecision(listing, nameof(DailyProductListing.OpeningQuantity), 18, 3);
        AssertPrecision(listing, nameof(DailyProductListing.CurrentQuantity), 18, 3);
        AssertPrecision(listing, nameof(DailyProductListing.ReservedQuantity), 18, 3);
        AssertPrecision(listing, nameof(DailyProductListing.AvailableQuantity), 18, 3);
        AssertPrecision(ledger, nameof(InventoryLedger.QuantityDelta), 18, 3);
        AssertPrecision(ledger, nameof(InventoryLedger.QuantityBefore), 18, 3);
        AssertPrecision(ledger, nameof(InventoryLedger.QuantityAfter), 18, 3);
        AssertPrecision(ledger, nameof(InventoryLedger.UnitPriceBefore), 18, 2);
        AssertPrecision(ledger, nameof(InventoryLedger.UnitPriceAfter), 18, 2);

        Assert.Equal(
            typeof(string),
            listing.FindProperty(nameof(DailyProductListing.Status))!.GetProviderClrType());
        Assert.Equal(
            typeof(string),
            ledger.FindProperty(nameof(InventoryLedger.TransactionType))!.GetProviderClrType());
        Assert.Equal(
            typeof(string),
            context.Model.FindEntityType(typeof(InventorySession))!
                .FindProperty(nameof(InventorySession.Status))!
                .GetProviderClrType());
    }

    [Fact]
    public void ConfigureDailyListing_WhenModelIsBuilt_UsesVersionAsConcurrencyToken()
    {
        using var context = CreateContext();

        var version = context.Model.FindEntityType(typeof(DailyProductListing))!
            .FindProperty(nameof(DailyProductListing.Version))!;

        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.Never, version.ValueGenerated);
    }

    [Fact]
    public void ConfigureInventoryEntities_WhenModelIsBuilt_UsesRestrictiveDeletes()
    {
        using var context = CreateContext();

        var session = context.Model.FindEntityType(typeof(InventorySession))!;
        var listing = context.Model.FindEntityType(typeof(DailyProductListing))!;
        var ledger = context.Model.FindEntityType(typeof(InventoryLedger))!;

        Assert.Equal(
            DeleteBehavior.Restrict,
            session.GetForeignKeys().Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Stall)).DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            listing.GetForeignKeys().Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(InventorySession)).DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            listing.GetForeignKeys().Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Haggly.Domain.Modules.Catalog.ProductStall)).DeleteBehavior);
        Assert.All(ledger.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Fact]
    public void ConfigureDailyListing_WhenModelIsBuilt_UsesQuantityCheckConstraints()
    {
        using var context = CreateContext();

        var listing = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(DailyProductListing))!;
        var constraintNames = listing.GetCheckConstraints().Select(constraint => constraint.Name).ToArray();

        Assert.Contains("CK_daily_product_listings_quantity_bounds", constraintNames);
        Assert.Contains("CK_daily_product_listings_available_quantity_bounds", constraintNames);
    }

    [Fact]
    public void ConfigureInventoryEntities_WhenModelIsBuilt_DoesNotDiscoverReservationOrSalesGraph()
    {
        using var context = CreateContext();

        Assert.Null(context.Model.FindEntityType(typeof(InventoryReservation)));
        Assert.DoesNotContain(context.Model.GetEntityTypes(), entityType =>
            entityType.ClrType.Namespace?.Contains("Modules.Sales", StringComparison.Ordinal) == true);
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
        Assert.Equal("inventory", metadata.GetSchema());
    }

    private static void AssertPrecision(
        Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType entityType,
        string propertyName,
        int precision,
        int scale)
    {
        var property = entityType.FindProperty(propertyName)!;
        Assert.Equal(precision, property.GetPrecision());
        Assert.Equal(scale, property.GetScale());
    }
}
