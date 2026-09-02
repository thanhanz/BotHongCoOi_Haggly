using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentTransactionTests
{
    [Fact]
    public void Create_AmountMatchesPayment_CreatesPendingTransaction()
    {
        // Arrange
        var payment = CreatePayment();

        // Act
        var transaction = PaymentTransaction.Create(TransactionId, payment, payment.AmountDue, CreatedAt);

        // Assert
        Assert.Equal(payment.Id, transaction.PaymentId);
        Assert.Same(payment, transaction.Payment);
        Assert.Equal(PaymentTransactionType.PAYMENT, transaction.TransactionType);
        Assert.Equal(PaymentTransactionStatus.PENDING, transaction.Status);
        Assert.Equal(payment.AmountDue, transaction.Amount);
    }

    [Fact]
    public void MarkSucceeded_PendingTransaction_RecordsProviderDataAndSucceeds()
    {
        // Arrange
        var transaction = CreateTransaction();

        // Act
        transaction.MarkSucceeded("  provider-123  ", "  OK ", " response ", ProcessedAt);

        // Assert
        Assert.Equal(PaymentTransactionStatus.SUCCEEDED, transaction.Status);
        Assert.Equal("provider-123", transaction.ProviderTransactionId);
        Assert.Equal("OK", transaction.ProviderResponseCode);
        Assert.Equal("response", transaction.ProviderResponseData);
        Assert.Equal(ProcessedAt, transaction.ProcessedAt);
        Assert.Null(transaction.FailureReason);
    }

    [Fact]
    public void MarkFailed_PendingTransaction_RecordsFailureAndFails()
    {
        // Arrange
        var transaction = CreateTransaction();

        // Act
        transaction.MarkFailed("  declined  ", "DECLINED", null, ProcessedAt);

        // Assert
        Assert.Equal(PaymentTransactionStatus.FAILED, transaction.Status);
        Assert.Equal("declined", transaction.FailureReason);
        Assert.Equal("DECLINED", transaction.ProviderResponseCode);
        Assert.Equal(ProcessedAt, transaction.ProcessedAt);
    }

    [Fact]
    public void MarkSucceeded_SucceededTransaction_RejectsRepeatedTransition()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.MarkSucceeded("provider-123", null, null, ProcessedAt);

        // Act
        var action = () => transaction.MarkSucceeded("provider-456", null, null, FailedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("provider-123", transaction.ProviderTransactionId);
        Assert.Equal(ProcessedAt, transaction.ProcessedAt);
    }

    [Fact]
    public void MarkFailed_SucceededTransaction_RejectsForbiddenTransition()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.MarkSucceeded("provider-123", null, null, ProcessedAt);

        // Act
        var action = () => transaction.MarkFailed("declined", null, null, FailedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(PaymentTransactionStatus.SUCCEEDED, transaction.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidAmount_RejectsTransaction(decimal amount)
    {
        // Arrange
        var payment = CreatePayment();

        // Act
        var action = () => PaymentTransaction.Create(TransactionId, payment, amount, CreatedAt);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Create_AmountDiffersFromPaymentDue_RejectsTransaction()
    {
        // Arrange
        var payment = CreatePayment();

        // Act
        var action = () => PaymentTransaction.Create(TransactionId, payment, 1m, CreatedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    private static PaymentTransaction CreateTransaction() =>
        PaymentTransaction.Create(TransactionId, CreatePayment(), 300_000m, CreatedAt);

    private static Payment CreatePayment() =>
        Payment.Create(PaymentId, OrderId, 300_000m, "VND", CreatedAt);

    private static readonly Guid PaymentId = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("62000000-0000-0000-0000-000000000002");
    private static readonly Guid TransactionId = Guid.Parse("62000000-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessedAt = new(2026, 8, 30, 6, 1, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FailedAt = new(2026, 8, 30, 6, 2, 0, TimeSpan.Zero);
}
