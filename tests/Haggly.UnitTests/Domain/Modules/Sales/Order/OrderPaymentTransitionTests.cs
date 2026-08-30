using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;
using DomainOrder = Haggly.Domain.Modules.Sales.Order;

namespace Haggly.UnitTests.Domain.Modules.Sales.Order;

public sealed class OrderPaymentTransitionTests
{
    [Fact]
    public void StartPayment_AgreedOrder_MovesToPaymentPending()
    {
        // Arrange
        var order = CreateAgreedOrder();
        var occurredAt = new DateTimeOffset(2026, 8, 17, 2, 5, 0, TimeSpan.Zero);

        // Act
        var changed = order.StartPayment(occurredAt);

        // Assert
        Assert.True(changed);
        Assert.Equal(OrderStatus.PAYMENT_PENDING, order.Status);
        Assert.Equal(occurredAt, order.UpdatedAt);
    }

    [Fact]
    public void ApplyFailedPayment_PaymentPendingOrder_ReturnsToAgreed()
    {
        // Arrange
        var order = CreateAgreedOrder();
        order.StartPayment(new DateTimeOffset(2026, 8, 17, 2, 5, 0, TimeSpan.Zero));
        var occurredAt = new DateTimeOffset(2026, 8, 17, 2, 6, 0, TimeSpan.Zero);

        // Act
        var changed = order.ApplyFailedPayment(occurredAt);

        // Assert
        Assert.True(changed);
        Assert.Equal(OrderStatus.AGREED, order.Status);
        Assert.Equal(occurredAt, order.UpdatedAt);
    }

    [Fact]
    public void ApplySuccessfulPayment_ExactAllocations_MarksOrderPaid()
    {
        // Arrange
        var order = CreateAgreedOrder();
        var fulfillment = Assert.Single(order.StallFulfillments);
        var occurredAt = new DateTimeOffset(2026, 8, 17, 2, 7, 0, TimeSpan.Zero);
        var allocation = new OrderPaymentAllocation(fulfillment.Id, fulfillment.StallId, fulfillment.FinalAmount);

        // Act
        var changed = order.ApplySuccessfulPayment([allocation], occurredAt);

        // Assert
        Assert.True(changed);
        Assert.Equal(OrderStatus.PAID, order.Status);
        Assert.Equal(order.TotalToCharge, order.TotalPaid);
    }

    private static DomainOrder CreateAgreedOrder()
    {
        var order = DomainOrder.Place(
            Guid.Parse("22000000-0000-0000-0000-000000000001"),
            Guid.Parse("22000000-0000-0000-0000-000000000002"),
            [new OrderItemInput(Guid.Parse("22000000-0000-0000-0000-000000000003"), Guid.Parse("22000000-0000-0000-0000-000000000004"), "Rice", ProductUnit.KG, 60_000m, 2m, null)],
            new DateTimeOffset(2026, 8, 17, 2, 0, 0, TimeSpan.Zero));
        order.Status = OrderStatus.AGREED;
        foreach (var fulfillment in order.StallFulfillments)
            fulfillment.Status = StallFulfillmentStatus.AGREED;
        return order;
    }
}
