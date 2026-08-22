using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Common.Events.V1;
using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Payments;

public sealed class ProcessPaymentRequestedConsumerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 22, 9, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public async Task ConsumeAsync_WhenProviderSucceeds_MarksPaymentAndAttemptSucceededAndWritesEvent()
    {
        var payment = CreatePayment();
        var repository = new FakePaymentCommandRepository(payment);
        var outbox = new FakeOutboxWriter();
        var consumer = CreateConsumer(
            repository,
            outbox,
            new FakePaymentProvider(new(true, "SIM-123", null)));

        await consumer.ConsumeAsync(CreateRequested(payment), CancellationToken.None);

        var transaction = Assert.Single(repository.Transactions);
        var succeeded = Assert.IsType<PaymentSucceeded>(Assert.Single(outbox.Events));
        Assert.Equal(PaymentStatus.PAID, payment.Status);
        Assert.Equal(PaymentTransactionStatus.SUCCEEDED, transaction.Status);
        Assert.Equal(transaction.Id, succeeded.PaymentTransactionId);
        Assert.Equal("SIM-123", succeeded.ProviderTransactionId);
        Assert.Equal(TimeSpan.Zero, succeeded.OccurredAt.Offset);
    }

    [Fact]
    public async Task ConsumeAsync_WhenProviderDeclines_MarksPaymentAndAttemptFailedAndWritesEvent()
    {
        var payment = CreatePayment();
        var repository = new FakePaymentCommandRepository(payment);
        var outbox = new FakeOutboxWriter();
        var consumer = CreateConsumer(
            repository,
            outbox,
            new FakePaymentProvider(new(false, null, "simulated decline")));

        await consumer.ConsumeAsync(CreateRequested(payment), CancellationToken.None);

        var transaction = Assert.Single(repository.Transactions);
        var failed = Assert.IsType<PaymentFailed>(Assert.Single(outbox.Events));
        Assert.Equal(PaymentStatus.FAILED, payment.Status);
        Assert.Equal(PaymentTransactionStatus.FAILED, transaction.Status);
        Assert.Equal("simulated decline", failed.FailureReason);
    }

    [Fact]
    public async Task ConsumeAsync_WhenPaymentIsAlreadyPaid_DoesNotInvokeProviderOrWriteAgain()
    {
        var payment = CreatePayment();
        payment.StartProcessing(Now);
        payment.MarkPaid(Now);
        var repository = new FakePaymentCommandRepository(payment);
        var provider = new FakePaymentProvider(new(true, "SIM-123", null));
        var outbox = new FakeOutboxWriter();
        var consumer = CreateConsumer(repository, outbox, provider);

        await consumer.ConsumeAsync(CreateRequested(payment), CancellationToken.None);

        Assert.Equal(0, provider.CallCount);
        Assert.Empty(repository.Transactions);
        Assert.Empty(outbox.Events);
    }

    private static ProcessPaymentRequestedConsumer CreateConsumer(
        IPaymentCommandRepository repository,
        IOutboxWriter outbox,
        IPaymentProvider provider)
        => new(repository, provider, outbox, new FakePaymentUnitOfWork(), new FixedBusinessClock(Now));

    private static Payment CreatePayment()
        => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 300_000m, "VND", Now);

    private static PaymentRequested CreateRequested(Payment payment)
        => new(
            Guid.NewGuid(),
            payment.Id,
            Now,
            payment.Id,
            payment.OrderId,
            payment.AmountDue,
            payment.Currency);

    private sealed class FakePaymentCommandRepository(Payment payment) : IPaymentCommandRepository
    {
        public List<PaymentTransaction> Transactions { get; } = [];

        public Task<Payment?> FindByIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => Task.FromResult(payment.Id == paymentId ? payment : null);

        public Task AddTransactionAsync(
            PaymentTransaction transaction,
            CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<PaymentOrderSnapshot?> FindOrderAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult<PaymentOrderSnapshot?>(null);

        public Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult<Payment?>(null);

        public Task AddAsync(Payment paymentToAdd, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakePaymentProvider(PaymentProviderResult result) : IPaymentProvider
    {
        public int CallCount { get; private set; }

        public Task<PaymentProviderResult> ProcessAsync(
            PaymentProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<IDomainEvent> Events { get; } = [];

        public Task WriteAsync<TEvent>(
            TEvent domainEvent,
            CancellationToken cancellationToken = default)
            where TEvent : class, IDomainEvent
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentUnitOfWork : IPaymentUnitOfWork
    {
        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
            => operation(cancellationToken);
    }

    private sealed class FixedBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(now.DateTime);
    }
}
