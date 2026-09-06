using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryPersistenceModelTests
{
    [Fact]
    public void ConfigureInventory_UsesContinuousTablesAndUniqueRelationships()
    {
        using var context = CreateContext();
        var inventory = context.Model.FindEntityType(typeof(DomainInventory))!;
        var item = context.Model.FindEntityType(typeof(InventoryItem))!;
        Assert.Equal("inventories", inventory.GetTableName());
        Assert.Equal("inventory_items", item.GetTableName());
        Assert.Contains(inventory.GetIndexes(), index => index.IsUnique
            && index.Properties.Single().Name == nameof(DomainInventory.StallId));
        Assert.Contains(item.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(p => p.Name).SequenceEqual(
                [nameof(InventoryItem.InventoryId), nameof(InventoryItem.ProductStallId)]));
    }

    [Fact]
    public void ConfigureInventoryItem_UsesQuantityConstraintAndConcurrencyToken()
    {
        using var context = CreateContext();
        var item = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(InventoryItem))!;
        Assert.Contains(item.GetCheckConstraints(), constraint => constraint.Name == "CK_inventory_items_quantity_bounds");
        Assert.True(item.FindProperty(nameof(InventoryItem.Version))!.IsConcurrencyToken);
        Assert.Null(item.FindProperty(nameof(InventoryItem.AvailableQuantity)));
    }

    [Fact]
    public void ConfigureProductStall_UsesCurrentPriceAndConcurrencyToken()
    {
        using var context = CreateContext();
        var productStall = context.Model.FindEntityType(typeof(ProductStall))!;
        Assert.NotNull(productStall.FindProperty(nameof(ProductStall.CurrentUnitPrice)));
        Assert.True(productStall.FindProperty(nameof(ProductStall.Version))!.IsConcurrencyToken);
        Assert.Null(productStall.FindProperty("DefaultUnitPrice"));
    }

    [Fact]
    public void ConfigureInventoryRelationships_UseRestrictiveDeletes()
    {
        using var context = CreateContext();
        var inventory = context.Model.FindEntityType(typeof(DomainInventory))!;
        var item = context.Model.FindEntityType(typeof(InventoryItem))!;
        Assert.Equal(DeleteBehavior.Restrict,
            inventory.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Stall)).DeleteBehavior);
        Assert.All(item.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    private static HagglyDbContext CreateContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres").Options);
}
