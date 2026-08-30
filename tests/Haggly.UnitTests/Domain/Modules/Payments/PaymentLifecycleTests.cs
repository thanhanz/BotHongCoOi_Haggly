using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentLifecycleTests
{
    [Fact]
    public void Create_ValidAmountAndCurrency_CreatesPendingPayment()
    {
        // Arrange

        // Act
        var payment = CreatePayment();

        // Assert
        Assert.Equal(PaymentStatus.PENDING, payment.Status);
        Assert.Equal(300_000m, payment.AmountDue);
        Assert.Equal(0m, payment.AmountPaid);
        Assert.Equal("VND", payment.Currency);
    }

    [Fact]
    public void StartProcessing_PendingPayment_MovesToProcessing()
    {
        // Arrange
        var payment = CreatePayment();

        // Act
        payment.StartProcessing(ProcessingAt);

        // Assert
        Assert.Equal(PaymentStatus.PROCESSING, payment.Status);
        Assert.Equal(ProcessingAt, payment.UpdatedAt);
    }

    [Fact]
    public void MarkPaid_ProcessingPayment_CollectsAmountAndCompletesPayment()
    {
        // Arrange
        var payment = CreatePayment();
        payment.StartProcessing(ProcessingAt);

        // Act
        payment.MarkPaid(PaidAt);

        // Assert
        Assert.Equal(PaymentStatus.PAID, payment.Status);
        Assert.Equal(payment.AmountDue, payment.AmountPaid);
        Assert.Equal(PaidAt, payment.CompletedAt);
    }

    [Fact]
    public void MarkFailed_ProcessingPayment_ClearsCollectedAmountAndFailsPayment()
    {
        // Arrange
        var payment = CreatePayment();
        payment.StartProcessing(ProcessingAt);

        // Act
        payment.MarkFailed(FailedAt);

        // Assert
        Assert.Equal(PaymentStatus.FAILED, payment.Status);
        Assert.Equal(0m, payment.AmountPaid);
        Assert.Null(payment.CompletedAt);
        Assert.Equal(FailedAt, payment.UpdatedAt);
    }

    [Fact]
    public void MarkPaid_PendingPayment_RejectsAndLeavesPaymentUnchanged()
    {
        // Arrange
        var payment = CreatePayment();

        // Act
        var action = () => payment.MarkPaid(PaidAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(PaymentStatus.PENDING, payment.Status);
        Assert.Equal(0m, payment.AmountPaid);
        Assert.Null(payment.UpdatedAt);
    }

    [Fact]
    public void MarkFailed_PaidPayment_RejectsAndLeavesPaymentUnchanged()
    {
        // Arrange
        var payment = CreatePayment();
        payment.StartProcessing(ProcessingAt);
        payment.MarkPaid(PaidAt);

        // Act
        var action = () => payment.MarkFailed(FailedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(PaymentStatus.PAID, payment.Status);
        Assert.Equal(payment.AmountDue, payment.AmountPaid);
        Assert.Equal(PaidAt, payment.CompletedAt);
    }

    [Fact]
    public void MarkPaid_PaidPayment_RejectsRepeatedTransition()
    {
        // Arrange
        var payment = CreatePayment();
        payment.StartProcessing(ProcessingAt);
        payment.MarkPaid(PaidAt);

        // Act
        var action = () => payment.MarkPaid(new DateTimeOffset(2026, 8, 30, 5, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(PaidAt, payment.CompletedAt);
    }

    [Fact]
    public void Create_InvalidAmountOrCurrency_RejectsPayment()
    {
        // Arrange

        // Act
        var zeroAmount = () => Payment.Create(PaymentId, OrderId, 0m, "VND", CreatedAt);
        var invalidCurrency = () => Payment.Create(PaymentId, OrderId, 1m, "VN", CreatedAt);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(zeroAmount);
        Assert.Throws<ArgumentException>(invalidCurrency);
    }

    private static Payment CreatePayment() =>
        Payment.Create(PaymentId, OrderId, 300_000m, "vnd", CreatedAt);

    private static readonly Guid PaymentId = Guid.Parse("61000000-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("61000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 30, 5, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessingAt = new(2026, 8, 30, 5, 1, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PaidAt = new(2026, 8, 30, 5, 2, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FailedAt = new(2026, 8, 30, 5, 3, 0, TimeSpan.Zero);
}
