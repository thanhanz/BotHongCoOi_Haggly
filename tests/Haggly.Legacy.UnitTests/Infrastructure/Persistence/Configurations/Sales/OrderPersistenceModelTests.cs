using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Markets;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Sales;

public sealed class OrderPersistenceModelTests
{
    [Fact]
    public void ConfigureOrders_UsesSalesTablesAndBuyerOwnershipIndex()
    {
        using var context = CreateContext();
        var order = context.Model.FindEntityType(typeof(Order))!;
        var fulfillment = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(StallFulfillment))!;
        var item = context.Model.FindEntityType(typeof(OrderItem))!;

        Assert.Equal("orders", order.GetTableName());
        Assert.Equal("sales", order.GetSchema());
        Assert.Equal("stall_fulfillments", fulfillment.GetTableName());
        Assert.Equal("order_items", item.GetTableName());
        Assert.Contains(order.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Order.OrderNo)]));
        Assert.Contains(order.GetIndexes(), index => index.Properties.Any(
            property => property.Name == nameof(Order.BuyerId)));
    }

    [Fact]
    public void ConfigureOrderChildren_UsesUniqueStallAndInventoryRelationships()
    {
        using var context = CreateContext();
        var fulfillment = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(StallFulfillment))!;
        var item = context.Model.FindEntityType(typeof(OrderItem))!;

        Assert.Contains(fulfillment.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(StallFulfillment.OrderId), nameof(StallFulfillment.StallId)]));
        Assert.Contains(item.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(OrderItem.StallFulfillmentId), nameof(OrderItem.InventoryItemId)]));
    }

    [Fact]
    public void ConfigureOrders_UsesRestrictiveExternalRelationshipsAndAmountConstraints()
    {
        using var context = CreateContext();
        var order = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Order))!;
        var fulfillment = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(StallFulfillment))!;
        var item = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(OrderItem))!;

        Assert.Equal(DeleteBehavior.Restrict,
            order.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(BuyerProfile)).DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict,
            fulfillment.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Stall)).DeleteBehavior);
        Assert.Contains(order.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_orders_amount_bounds");
        Assert.Contains(item.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_order_items_amount_bounds");
    }

    private static HagglyDbContext CreateContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres").Options);
}
