using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Common.Events.V1;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
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
        var repository = new FakePaymentCommandRepository
        {
            Order = new PaymentOrderSnapshot(orderId, buyerId, OrderStatus.AGREED, 300_000m, "VND")
        };
        var outbox = new FakeOutboxWriter();
        var unitOfWork = new FakePaymentUnitOfWork();
        var handler = new StartPaymentHandler(
            repository, outbox, unitOfWork, new FixedBusinessClock(Now));

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
        Assert.Equal(1, unitOfWork.TransactionCount);
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotEligible_ThrowsPaymentConflictException()
    {
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var repository = new FakePaymentCommandRepository
        {
            Order = new PaymentOrderSnapshot(orderId, buyerId, OrderStatus.NEGOTIATING, 300_000m, "VND")
        };
        var handler = new StartPaymentHandler(
            repository,
            new FakeOutboxWriter(),
            new FakePaymentUnitOfWork(),
            new FixedBusinessClock(Now));

        await Assert.ThrowsAsync<PaymentConflictException>(() =>
            handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenOrderBelongsToAnotherBuyer_ThrowsPaymentForbiddenException()
    {
        var orderId = Guid.NewGuid();
        var repository = new FakePaymentCommandRepository
        {
            Order = new PaymentOrderSnapshot(
                orderId,
                Guid.NewGuid(),
                OrderStatus.AGREED,
                300_000m,
                "VND")
        };
        var handler = new StartPaymentHandler(
            repository,
            new FakeOutboxWriter(),
            new FakePaymentUnitOfWork(),
            new FixedBusinessClock(Now));

        await Assert.ThrowsAsync<PaymentForbiddenException>(() =>
            handler.Handle(
                new StartPaymentCommand(orderId, Guid.NewGuid()),
                CancellationToken.None));
    }

    private sealed class FakePaymentCommandRepository : IPaymentCommandRepository
    {
        public PaymentOrderSnapshot? Order { get; init; }
        public Payment? ExistingPayment { get; init; }
        public List<Payment> Payments { get; } = [];
        public int SaveCount { get; private set; }

        public Task<PaymentOrderSnapshot?> FindOrderAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult(Order?.OrderId == orderId ? Order : null);

        public Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingPayment?.OrderId == orderId ? ExistingPayment : null);

        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
        {
            Payments.Add(payment);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
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
