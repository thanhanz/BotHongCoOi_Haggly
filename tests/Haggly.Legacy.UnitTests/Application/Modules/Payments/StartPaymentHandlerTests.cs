using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Common.Events.V1;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Haggly.Domain.Modules.Catalog;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Payments;

public sealed class StartPaymentHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 21, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenOrderIsAgreed_CreatesPendingPaymentAndWritesRequestedEvent()
    {
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var order = CreateOrder(orderId, buyerId, OrderStatus.AGREED);
        var repository = new FakePaymentCommandRepository { Order = order };
        var outbox = new FakeOutboxWriter();
        var unitOfWork = new FakePaymentUnitOfWork();
        var inventoryRepository = new FakeInventoryPaymentRepository();
        var handler = new StartPaymentHandler(
            repository,
            new FakeOrderCommandRepository(order),
            inventoryRepository,
            outbox,
            unitOfWork,
            new FixedBusinessClock(Now));

        var result = await handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None);

        var payment = Assert.Single(repository.Payments);
        var requested = Assert.IsType<PaymentRequested>(Assert.Single(outbox.Events));
        Assert.Equal(PaymentStatus.PENDING, result.Status);
        Assert.Equal(payment.Id, requested.PaymentId);
        Assert.Equal(orderId, requested.OrderId);
        Assert.Equal(300_000m, requested.Amount);
        Assert.Equal("VND", requested.Currency);
        Assert.Equal(payment.Id, requested.CorrelationId);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(orderId, Assert.Single(inventoryRepository.ReservedOrderIds));
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(OrderStatus.PAYMENT_PENDING, order.Status);
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotEligible_ThrowsPaymentConflictException()
    {
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var repository = new FakePaymentCommandRepository
        {
            Order = CreateOrder(orderId, buyerId, OrderStatus.NEGOTIATING)
        };
        var handler = new StartPaymentHandler(
            repository,
            new FakeOrderCommandRepository(repository.Order!),
            new FakeInventoryPaymentRepository(),
            new FakeOutboxWriter(),
            new FakePaymentUnitOfWork(),
            new FixedBusinessClock(Now));

        await Assert.ThrowsAsync<PaymentConflictException>(() =>
            handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenInventoryCannotBeReserved_DoesNotCreatePaymentOrWriteEvent()
    {
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var repository = new FakePaymentCommandRepository
        {
            Order = CreateOrder(orderId, buyerId, OrderStatus.AGREED)
        };
        var inventoryRepository = new FakeInventoryPaymentRepository
        {
            ReserveException = new InvalidOperationException("insufficient stock")
        };
        var outbox = new FakeOutboxWriter();
        var handler = new StartPaymentHandler(
            repository,
            new FakeOrderCommandRepository(repository.Order!),
            inventoryRepository,
            outbox,
            new FakePaymentUnitOfWork(),
            new FixedBusinessClock(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None));

        Assert.Empty(repository.Payments);
        Assert.Empty(outbox.Events);
    }

    [Fact]
    public async Task Handle_WhenOrderBelongsToAnotherBuyer_ThrowsPaymentForbiddenException()
    {
        var orderId = Guid.NewGuid();
        var repository = new FakePaymentCommandRepository
        {
            Order = CreateOrder(
                orderId,
                Guid.NewGuid(),
                OrderStatus.AGREED)
        };
        var handler = new StartPaymentHandler(
            repository,
            new FakeOrderCommandRepository(repository.Order!),
            new FakeInventoryPaymentRepository(),
            new FakeOutboxWriter(),
            new FakePaymentUnitOfWork(),
            new FixedBusinessClock(Now));

        await Assert.ThrowsAsync<PaymentForbiddenException>(() =>
            handler.Handle(
                new StartPaymentCommand(orderId, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenClockHasNonUtcOffset_WritesUtcEventMetadata()
    {
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var localTimestamp = new DateTimeOffset(2026, 8, 22, 8, 0, 0, TimeSpan.FromHours(7));
        var repository = new FakePaymentCommandRepository
        {
            Order = CreateOrder(orderId, buyerId, OrderStatus.AGREED)
        };
        var outbox = new FakeOutboxWriter();
        var handler = new StartPaymentHandler(
            repository,
            new FakeOrderCommandRepository(repository.Order!),
            new FakeInventoryPaymentRepository(),
            outbox,
            new FakePaymentUnitOfWork(),
            new FixedBusinessClock(localTimestamp));

        await handler.Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);

        var requested = Assert.IsType<PaymentRequested>(Assert.Single(outbox.Events));
        Assert.Equal(TimeSpan.Zero, requested.OccurredAt.Offset);
        Assert.Equal(localTimestamp.UtcDateTime, requested.OccurredAt.UtcDateTime);
    }

    private static Order CreateOrder(Guid orderId, Guid buyerId, OrderStatus status)
    {
        var order = Order.Place(
            orderId,
            buyerId,
            [new OrderItemInput(
                Guid.NewGuid(), Guid.NewGuid(), "Rice", ProductUnit.KG,
                300_000m, 1m, null)],
            Now);
        order.Status = status;
        return order;
    }

    private sealed class FakePaymentCommandRepository : IPaymentCommandRepository
    {
        public Order? Order { get; init; }
        public Payment? ExistingPayment { get; init; }
        public List<Payment> Payments { get; } = [];
        public int SaveCount { get; private set; }

        public Task<Payment?> FindByIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => Task.FromResult(
                Payments.Concat(ExistingPayment is null ? [] : [ExistingPayment])
                    .SingleOrDefault(payment => payment.Id == paymentId));

        public Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingPayment?.OrderId == orderId ? ExistingPayment : null);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
        {
            Payments.Add(payment);
            return Task.CompletedTask;
        }

        public Task AddTransactionAsync(
            PaymentTransaction transaction,
            CancellationToken cancellationToken)
            => Task.CompletedTask;


        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrderCommandRepository(Order order) : IOrderCommandRepository
    {
        public Task<Order?> FindByIdAsync(
            Guid orderId,
            CancellationToken cancellationToken)
            => Task.FromResult<Order?>(order.Id == orderId ? order : null);

        public Task AddAsync(Order orderToAdd, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<IDomainEvent> Events { get; } = [];

        public Task WriteAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : class, IDomainEvent
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryPaymentRepository : IInventoryPaymentRepository
    {
        public List<Guid> ReservedOrderIds { get; } = [];
        public Exception? ReserveException { get; init; }

        public Task ReserveAsync(
            Guid orderId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
        {
            if (ReserveException is not null)
                throw ReserveException;

            ReservedOrderIds.Add(orderId);
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(
            Guid orderId,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

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

    private sealed class FakePaymentUnitOfWork : IPaymentUnitOfWork
    {
        public int TransactionCount { get; private set; }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            TransactionCount++;
            return await operation(cancellationToken);
        }
    }

    private sealed class FixedBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(now.DateTime);
    }
}
