using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Common.Events.V1;
using Haggly.Domain.Modules.Payments;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Payments.ProcessPaymentRequested;

public sealed class ProcessPaymentRequestedHandlerTests
{
    private readonly IPaymentCommandRepository _repository = Substitute.For<IPaymentCommandRepository>();

    private readonly IPaymentProvider _paymentProvider = Substitute.For<IPaymentProvider>();

    private readonly IPaymentAllocationRepository _allocationRepository = Substitute.For<IPaymentAllocationRepository>();

    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();

    private readonly IPaymentUnitOfWork _unitOfWork = Substitute.For<IPaymentUnitOfWork>();

    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task HandleAsync_ProviderSucceeds_MarksPaymentPaidAndPublishesSuccess()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigurePayment(fixture.Payment);
        _paymentProvider.ProcessAsync(
                Arg.Any<PaymentProviderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderResult(true, "SIM-123", null));
        _allocationRepository.GetTargetsForOrderAsync(
                fixture.Payment.OrderId,
                Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<PaymentAllocationTarget>>(
            [new(fixture.FulfillmentId, fixture.StallId, fixture.Payment.AmountDue)]);
        PaymentTransaction? createdTransaction = null;
        _repository.AddTransactionAsync(
                Arg.Do<PaymentTransaction>(transaction => createdTransaction = transaction),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(CreateRequested(fixture), CancellationToken.None);

        // Assert
        Assert.Equal(PaymentStatus.PAID, fixture.Payment.Status);
        Assert.Equal(PaymentTransactionStatus.SUCCEEDED, createdTransaction!.Status);
        Assert.Equal("SIM-123", createdTransaction.ProviderTransactionId);
        await _paymentProvider.Received(1).ProcessAsync(
            Arg.Is<PaymentProviderRequest>(request =>
                request.PaymentId == fixture.Payment.Id
                && request.Amount == fixture.Payment.AmountDue
                && request.Currency == "VND"),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _outboxWriter.Received(1).WriteAsync(
            Arg.Is<PaymentSucceededEvent>(message =>
                message.PaymentId == fixture.Payment.Id
                && message.PaymentTransactionId == createdTransaction.Id
                && message.ProviderTransactionId == "SIM-123"
                && message.PaymentAllocationIds.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderDeclines_MarksPaymentFailedAndPublishesFailure()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigurePayment(fixture.Payment);
        _paymentProvider.ProcessAsync(
                Arg.Any<PaymentProviderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderResult(false, null, "declined"));
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(CreateRequested(fixture), CancellationToken.None);

        // Assert
        Assert.Equal(PaymentStatus.FAILED, fixture.Payment.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _outboxWriter.Received(1).WriteAsync(
            Arg.Is<PaymentFailedEvent>(message =>
                message.PaymentId == fixture.Payment.Id
                && message.FailureReason == "declined"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PaymentAlreadyPaid_DoesNotProcessOrPublish()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Payment.StartProcessing(fixture.Now);
        fixture.Payment.MarkPaid(fixture.Now);
        ConfigurePayment(fixture.Payment);
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(CreateRequested(fixture), CancellationToken.None);

        // Assert
        await _paymentProvider.DidNotReceive().ProcessAsync(
            Arg.Any<PaymentProviderRequest>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddTransactionAsync(
            Arg.Any<PaymentTransaction>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentSucceededEvent>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PaymentDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var fixture = CreateFixture();
        _repository.FindByIdAsync(fixture.Payment.Id, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().HandleAsync(
            CreateRequested(fixture), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PaymentNotFoundException>(action);
        await _paymentProvider.DidNotReceive().ProcessAsync(
            Arg.Any<PaymentProviderRequest>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentSucceededEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProviderThrows_PropagatesFailureWithoutEvent()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigurePayment(fixture.Payment);
        _paymentProvider.ProcessAsync(
                Arg.Any<PaymentProviderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentProviderResult>(
                new TimeoutException("provider timeout")));
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().HandleAsync(
            CreateRequested(fixture), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<TimeoutException>(action);
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentSucceededEvent>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ForwardsTokenToRepository()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _repository.FindByIdAsync(fixture.Payment.Id, cancellationToken)
            .Returns(Task.FromCanceled<Payment?>(cancellationToken));
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().HandleAsync(
            CreateRequested(fixture), cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _repository.Received(1).FindByIdAsync(
            fixture.Payment.Id, cancellationToken);
        await _paymentProvider.DidNotReceive().ProcessAsync(
            Arg.Any<PaymentProviderRequest>(), Arg.Any<CancellationToken>());
    }

    private ProcessPaymentRequestedHandler CreateSubject()
        => new(
            _repository,
            _paymentProvider,
            _allocationRepository,
            _outboxWriter,
            _unitOfWork,
            _clock);

    private void ConfigurePayment(Payment payment)
    {
        _repository.FindByIdAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(payment);
        _clock.GetNow().Returns(Now);
    }

    private void ConfigureTransaction()
        => _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<bool>>>();
                var cancellationToken = callInfo.ArgAt<CancellationToken>(1);
                return operation(cancellationToken);
            });

    private static PaymentRequested CreateRequested(PaymentFixture fixture)
        => new(
            Guid.Parse("90000000-0000-0000-0000-000000000004"),
            Guid.Parse("90000000-0000-0000-0000-000000000005"),
            fixture.Now,
            fixture.Payment.Id,
            fixture.Payment.OrderId,
            fixture.Payment.AmountDue,
            fixture.Payment.Currency);

    private static PaymentFixture CreateFixture()
    {
        var payment = Payment.Create(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            Guid.Parse("90000000-0000-0000-0000-000000000002"),
            300_000m,
            "VND",
            Now);
        return new PaymentFixture(
            payment,
            Guid.Parse("90000000-0000-0000-0000-000000000003"),
            Guid.Parse("90000000-0000-0000-0000-000000000006"),
            Now);
    }

    private static readonly DateTimeOffset Now =
        new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    private sealed record PaymentFixture(
        Payment Payment,
        Guid FulfillmentId,
        Guid StallId,
        DateTimeOffset Now);
}
