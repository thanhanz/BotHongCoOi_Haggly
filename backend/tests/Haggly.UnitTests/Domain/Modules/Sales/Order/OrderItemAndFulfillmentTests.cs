using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.Order;

public sealed class OrderItemAndFulfillmentTests
{
    [Fact]
    public void SetFinalValues_ValidNegotiation_RecalculatesLineAndFulfillmentAmounts()
    {
        // Arrange
        var order = CreateOrder();
        var fulfillment = Assert.Single(order.StallFulfillments);
        var item = Assert.Single(fulfillment.OrderItems);

        // Act
        item.SetFinalValues(2.5m, 12.345m, true);
        fulfillment.RecalculateAmounts();

        // Assert
        Assert.Equal(30.86m, item.LineTotal);
        Assert.True(item.IsNegotiated);
        Assert.Equal(30.86m, fulfillment.Subtotal);
        Assert.Equal(30.86m, fulfillment.FinalAmount);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, -1)]
    public void SetFinalValues_InvalidQuantityOrPrice_RejectsWithoutMutation(decimal quantity, decimal price)
    {
        // Arrange
        var item = Assert.Single(Assert.Single(CreateOrder().StallFulfillments).OrderItems);
        var originalQuantity = item.FinalQuantity;
        var originalPrice = item.FinalUnitPrice;

        // Act
        var action = () => item.SetFinalValues(quantity, price, true);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(originalQuantity, item.FinalQuantity);
        Assert.Equal(originalPrice, item.FinalUnitPrice);
    }

    [Fact]
    public void RecalculateAmounts_CancelledItem_ExcludesCancelledLine()
    {
        // Arrange
        var order = Haggly.Domain.Modules.Sales.Order.Place(
            OrderId, BuyerId,
            [
                CreateInput(InventoryItemId, 10m, 2m),
                CreateInput(OtherInventoryItemId, 5m, 3m)
            ], OccurredAt);
        var fulfillment = Assert.Single(order.StallFulfillments);
        fulfillment.OrderItems.Last().Status = OrderItemStatus.CANCELLED;

        // Act
        fulfillment.RecalculateAmounts();

        // Assert
        Assert.Equal(20m, fulfillment.Subtotal);
        Assert.Equal(20m, fulfillment.FinalAmount);
    }

    private static Haggly.Domain.Modules.Sales.Order CreateOrder()
        => Haggly.Domain.Modules.Sales.Order.Place(
            OrderId, BuyerId, [CreateInput(InventoryItemId, 10m, 2m)], OccurredAt);

    private static OrderItemInput CreateInput(Guid inventoryItemId, decimal price, decimal quantity)
        => new(inventoryItemId, StallId, "Apple", ProductUnit.KG, price, quantity, null);

    private static readonly Guid OrderId = Guid.Parse("22000000-0000-0000-0000-000000000001");
    private static readonly Guid BuyerId = Guid.Parse("22000000-0000-0000-0000-000000000002");
    private static readonly Guid StallId = Guid.Parse("22000000-0000-0000-0000-000000000003");
    private static readonly Guid InventoryItemId = Guid.Parse("22000000-0000-0000-0000-000000000004");
    private static readonly Guid OtherInventoryItemId = Guid.Parse("22000000-0000-0000-0000-000000000005");
    private static readonly DateTimeOffset OccurredAt = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}
