using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales;

public sealed class OrderPaymentFailedHandlerTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenEventIsNew_MovesPaymentPendingOrderBackToAgreed()
    {
        var order = CreateOrder(OrderStatus.PAYMENT_PENDING);
        var repository = new FakeOrderCommandRepository(order);
        var inbox = new FakeInboxRepository(true);
        var handler = CreateHandler(repository, inbox);

        await handler.HandleAsync(CreateEvent(order), CancellationToken.None);

        Assert.Equal(OrderStatus.AGREED, order.Status);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task HandleAsync_WhenEventIsDuplicate_DoesNotLoadOrChangeOrder()
    {
        var order = CreateOrder(OrderStatus.PAYMENT_PENDING);
        var repository = new FakeOrderCommandRepository(order);
        var handler = CreateHandler(repository, new FakeInboxRepository(false));

        await handler.HandleAsync(CreateEvent(order), CancellationToken.None);

        Assert.Equal(0, repository.FindCount);
        Assert.Equal(0, repository.SaveCount);
        Assert.Equal(OrderStatus.PAYMENT_PENDING, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.PAID)]
    [InlineData(OrderStatus.CANCELLED)]
    public async Task HandleAsync_WhenFailureIsDelayed_DoesNotOverwriteTerminalOrder(
        OrderStatus status)
    {
        var order = CreateOrder(status);
        var repository = new FakeOrderCommandRepository(order);
        var handler = CreateHandler(repository, new FakeInboxRepository(true));

        await handler.HandleAsync(CreateEvent(order), CancellationToken.None);

        Assert.Equal(status, order.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    private static OrderPaymentFailedHandler CreateHandler(
        IOrderCommandRepository repository,
        IInboxRepository inbox)
        => new(
            repository,
            inbox,
            new InlineSalesTransactionExecutor(),
            new FixedBusinessClock(OccurredAt.AddMinutes(1)));

    private static Order CreateOrder(OrderStatus status)
    {
        var order = Order.Place(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderItemInput(
                Guid.NewGuid(), Guid.NewGuid(), "Rice", ProductUnit.KG,
                50_000m, 2m, null)],
            OccurredAt.AddMinutes(-1));
        order.Status = status;
        foreach (var fulfillment in order.StallFulfillments)
            fulfillment.Status = StallFulfillmentStatus.AGREED;
        return order;
    }

    private static PaymentFailedEvent CreateEvent(Order order)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OccurredAt,
            Guid.NewGuid(),
            Guid.NewGuid(),
            order.Id,
            order.TotalToCharge,
            order.Currency,
            "simulated decline");

    private sealed class FakeOrderCommandRepository(Order order) : IOrderCommandRepository
    {
        public int FindCount { get; private set; }
        public int SaveCount { get; private set; }

        public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken)
        {
            FindCount++;
            return Task.FromResult<Order?>(order.Id == orderId ? order : null);
        }

        public Task AddAsync(Order orderToAdd, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInboxRepository(bool added) : IInboxRepository
    {
        public Task<bool> TryAddAsync(
            string consumerName,
            Guid eventId,
            string eventType,
            DateTimeOffset processedAt,
            CancellationToken cancellationToken)
        {
            Assert.Equal("order-payment-failed-v1", consumerName);
            Assert.Equal("payments.payment-failed.v1", eventType);
            Assert.Equal(OccurredAt.AddMinutes(1), processedAt);
            return Task.FromResult(added);
        }
    }

    private sealed class InlineSalesTransactionExecutor : ISalesTransactionExecutor
    {
        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
            => operation(cancellationToken);
    }

    private sealed class FixedBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;

        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(now.Date);
    }
}
