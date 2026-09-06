using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.PaymentResults;

public sealed class OrderPaymentSucceededHandlerTests
{
    private readonly IOrderCommandRepository _orders = Substitute.For<IOrderCommandRepository>();
    private readonly IPaymentAllocationRepository _allocations = Substitute.For<IPaymentAllocationRepository>();

    [Fact]
    public async Task HandleAsync_MatchingPaymentAllocations_MarksOrderPaidAndSaves()
    {
        // Arrange
        var fixture = CreateFixture();
        var allocation = PaymentAllocation.CreateSale(Guid.Parse("F3000000-0000-0000-0000-000000000001"), fixture.TransactionId, fixture.FulfillmentId, fixture.StallId, fixture.Order.TotalToCharge, Now);
        Configure(fixture, allocation);

        // Act
        await CreateSubject().HandleAsync(CreateMessage(fixture, allocation.Id), CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.PAID, fixture.Order.Status);
        Assert.Equal(fixture.Order.TotalToCharge, fixture.Order.TotalPaid);
        await _orders.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AllocationBelongsToAnotherTransaction_ThrowsWithoutSaving()
    {
        // Arrange
        var fixture = CreateFixture();
        var allocation = PaymentAllocation.CreateSale(Guid.Parse("F3000000-0000-0000-0000-000000000002"), Guid.Parse("F3000000-0000-0000-0000-000000000009"), fixture.FulfillmentId, fixture.StallId, fixture.Order.TotalToCharge, Now);
        Configure(fixture, allocation);

        // Act
        var action = () => CreateSubject().HandleAsync(CreateMessage(fixture, allocation.Id), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal(OrderStatus.AGREED, fixture.Order.Status);
        await _orders.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private OrderPaymentSucceededHandler CreateSubject() => new(_orders, _allocations);
    private void Configure(SalesFixture fixture, PaymentAllocation allocation)
    {
        _orders.FindByIdAsync(fixture.Order.Id, Arg.Any<CancellationToken>()).Returns(fixture.Order);
        _allocations.FindByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns<IReadOnlyList<PaymentAllocation>>([allocation]);
    }
    private static PaymentSucceededEvent CreateMessage(SalesFixture fixture, Guid allocationId) => new(Guid.Parse("F4000000-0000-0000-0000-000000000001"), Guid.Parse("F4000000-0000-0000-0000-000000000002"), Now, Guid.Parse("F4000000-0000-0000-0000-000000000003"), fixture.TransactionId, fixture.Order.Id, fixture.Order.TotalToCharge, "VND", "SIM-1", [allocationId]);
    private static SalesFixture CreateFixture()
    {
        var order = Order.Place(Guid.Parse("F4000000-0000-0000-0000-000000000004"), Guid.Parse("F4000000-0000-0000-0000-000000000005"), [new OrderItemInput(Guid.Parse("F4000000-0000-0000-0000-000000000006"), Guid.Parse("F4000000-0000-0000-0000-000000000007"), "Rice", ProductUnit.KG, 300_000m, 1m, null)], Now);
        order.Status = OrderStatus.AGREED;
        var fulfillment = Assert.Single(order.StallFulfillments);
        fulfillment.Status = StallFulfillmentStatus.AGREED;
        return new SalesFixture(order, Guid.Parse("F4000000-0000-0000-0000-000000000008"), fulfillment.Id, fulfillment.StallId);
    }
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 16, 0, 0, TimeSpan.Zero);
    private sealed record SalesFixture(Order Order, Guid TransactionId, Guid FulfillmentId, Guid StallId);
}
