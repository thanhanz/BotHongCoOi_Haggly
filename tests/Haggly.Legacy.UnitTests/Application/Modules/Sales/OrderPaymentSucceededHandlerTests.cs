using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales;

public sealed class OrderPaymentSucceededHandlerTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 24, 9, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public async Task HandleAsync_WhenEventAndAllocationsMatch_MarksOrderPaidAndSavesOnce()
    {
        var order = CreateAgreedOrder();
        var transactionId = Guid.NewGuid();
        var allocations = CreateAllocations(order, transactionId);
        var orderRepository = new FakeOrderCommandRepository(order);
        var handler = new OrderPaymentSucceededHandler(
            orderRepository,
            new FakePaymentAllocationRepository(allocations));

        await handler.HandleAsync(CreateEvent(order, allocations, transactionId), CancellationToken.None);

        Assert.Equal(OrderStatus.PAID, order.Status);
        Assert.Equal(order.TotalToCharge, order.TotalPaid);
        Assert.Equal(1, orderRepository.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenExactEventIsDeliveredTwice_DoesNotSaveAgain()
    {
        var order = CreateAgreedOrder();
        var transactionId = Guid.NewGuid();
        var allocations = CreateAllocations(order, transactionId);
        var orderRepository = new FakeOrderCommandRepository(order);
        var handler = new OrderPaymentSucceededHandler(
            orderRepository,
            new FakePaymentAllocationRepository(allocations));
        var message = CreateEvent(order, allocations, transactionId);

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(1, orderRepository.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenAllocationBelongsToAnotherTransaction_ThrowsWithoutSaving()
    {
        var order = CreateAgreedOrder();
        var allocations = CreateAllocations(order, Guid.NewGuid());
        var orderRepository = new FakeOrderCommandRepository(order);
        var handler = new OrderPaymentSucceededHandler(
            orderRepository,
            new FakePaymentAllocationRepository(allocations));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            CreateEvent(order, allocations, Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal(OrderStatus.AGREED, order.Status);
        Assert.Equal(0, orderRepository.SaveCount);
    }

    private static Order CreateAgreedOrder()
    {
        var order = Order.Place(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new OrderItemInput(
                    Guid.NewGuid(), Guid.NewGuid(), "Tomato", ProductUnit.KG,
                    60_000m, 2m, null),
                new OrderItemInput(
                    Guid.NewGuid(), Guid.NewGuid(), "Fish", ProductUnit.KG,
                    120_000m, 1.5m, null)
            ],
            OccurredAt);
        order.Status = OrderStatus.AGREED;
        foreach (var fulfillment in order.StallFulfillments)
            fulfillment.Status = StallFulfillmentStatus.AGREED;
        return order;
    }

    private static PaymentAllocation[] CreateAllocations(Order order, Guid transactionId)
        => order.StallFulfillments.Select(fulfillment => PaymentAllocation.CreateSale(
            Guid.NewGuid(),
            transactionId,
            fulfillment.Id,
            fulfillment.StallId,
            fulfillment.FinalAmount,
            OccurredAt)).ToArray();

    private static PaymentSucceededEvent CreateEvent(
        Order order,
        IReadOnlyList<PaymentAllocation> allocations,
        Guid transactionId)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OccurredAt,
            Guid.NewGuid(),
            transactionId,
            order.Id,
            order.TotalToCharge,
            order.Currency,
            "SIM-ORDER",
            allocations.Select(allocation => allocation.Id).ToArray());

    private sealed class FakeOrderCommandRepository(Order order) : IOrderCommandRepository
    {
        public int SaveCount { get; private set; }

        public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult<Order?>(order.Id == orderId ? order : null);

        public Task AddAsync(Order orderToAdd, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentAllocationRepository(
        IReadOnlyList<PaymentAllocation> allocations) : IPaymentAllocationRepository
    {
        public Task<IReadOnlyList<PaymentAllocationTarget>> GetTargetsForOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PaymentAllocationTarget>>([]);

        public Task<IReadOnlyList<PaymentAllocation>> FindByIdsAsync(
            IReadOnlyCollection<Guid> allocationIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PaymentAllocation>>(
                allocations.Where(allocation => allocationIds.Contains(allocation.Id)).ToArray());

        public Task AddAsync(PaymentAllocation allocation, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
