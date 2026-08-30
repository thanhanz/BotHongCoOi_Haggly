using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Inventory;

public sealed class InventoryPaymentFailedHandlerTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenEventIsNew_ReleasesReservedInventory()
    {
        var message = CreateEvent();
        var inbox = new FakeInboxRepository(true);
        var inventory = new FakeInventoryPaymentRepository();
        var handler = CreateHandler(inbox, inventory);

        await handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(message.EventId, Assert.Single(inbox.EventIds));
        Assert.Equal(message.OrderId, Assert.Single(inventory.ReleasedOrderIds));
    }

    [Fact]
    public async Task HandleAsync_WhenEventIsDuplicate_DoesNotReleaseInventoryAgain()
    {
        var inbox = new FakeInboxRepository(false);
        var inventory = new FakeInventoryPaymentRepository();
        var handler = CreateHandler(inbox, inventory);

        await handler.HandleAsync(CreateEvent(), CancellationToken.None);

        Assert.Empty(inventory.ReleasedOrderIds);
    }

    private static InventoryPaymentFailedHandler CreateHandler(
        IInboxRepository inbox,
        IInventoryPaymentRepository inventory)
        => new(
            inbox,
            inventory,
            new InlineInventoryUnitOfWork(),
            new FixedBusinessClock(OccurredAt.AddMinutes(1)));

    private static PaymentFailedEvent CreateEvent()
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OccurredAt,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            150_000m,
            "VND",
            "simulated decline");

    private sealed class FakeInboxRepository(bool added) : IInboxRepository
    {
        public List<Guid> EventIds { get; } = [];

        public Task<bool> TryAddAsync(
            string consumerName,
            Guid eventId,
            string eventType,
            DateTimeOffset processedAt,
            CancellationToken cancellationToken)
        {
            EventIds.Add(eventId);
            Assert.Equal("inventory-payment-failed-v1", consumerName);
            Assert.Equal("payments.payment-failed.v1", eventType);
            Assert.Equal(OccurredAt.AddMinutes(1), processedAt);
            return Task.FromResult(added);
        }
    }

    private sealed class FakeInventoryPaymentRepository : IInventoryPaymentRepository
    {
        public List<Guid> ReleasedOrderIds { get; } = [];

        public Task ReserveAsync(
            Guid orderId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ReleaseAsync(
            Guid orderId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
        {
            ReleasedOrderIds.Add(orderId);
            Assert.Equal(OccurredAt, occurredAt);
            return Task.CompletedTask;
        }

        public Task<bool> HasProcessedAsync(
            Guid paymentTransactionId,
            InventoryTransactionType transactionType,
            CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<IReadOnlyList<OrderItem>> FindActiveOrderItemsAsync(
            Guid orderId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OrderItem>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class InlineInventoryUnitOfWork : IInventoryUnitOfWork
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
