using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentTransactionDomainTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 22, 8, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public void Create_WhenAmountMatchesPayment_CreatesPendingPaymentAttemptInUtc()
    {
        var payment = CreatePayment();

        var transaction = PaymentTransaction.Create(
            Guid.NewGuid(), payment, payment.AmountDue, CreatedAt);

        Assert.Equal(payment.Id, transaction.PaymentId);
        Assert.Equal(PaymentTransactionType.PAYMENT, transaction.TransactionType);
        Assert.Equal(PaymentTransactionStatus.PENDING, transaction.Status);
        Assert.Equal(payment.AmountDue, transaction.Amount);
        Assert.Equal(TimeSpan.Zero, transaction.CreatedAt.Offset);
    }

    [Fact]
    public void Create_WhenAmountDiffersFromPayment_ThrowsInvalidOperationException()
    {
        var payment = CreatePayment();

        Assert.Throws<InvalidOperationException>(() => PaymentTransaction.Create(
            Guid.NewGuid(), payment, payment.AmountDue - 1m, CreatedAt));
    }

    [Fact]
    public void MarkSucceeded_WhenPending_CompletesOnceInUtc()
    {
        var transaction = CreateTransaction();
        var processedAt = CreatedAt.AddMinutes(1);

        transaction.MarkSucceeded("SIM-123", "APPROVED", null, processedAt);

        Assert.Equal(PaymentTransactionStatus.SUCCEEDED, transaction.Status);
        Assert.Equal("SIM-123", transaction.ProviderTransactionId);
        Assert.Equal(TimeSpan.Zero, transaction.ProcessedAt!.Value.Offset);
        Assert.Throws<InvalidOperationException>(() =>
            transaction.MarkFailed("late failure", null, null, processedAt.AddMinutes(1)));
    }

    [Fact]
    public void MarkFailed_WhenPending_RecordsTrimmedFailureAndCannotCompleteAgain()
    {
        var transaction = CreateTransaction();

        transaction.MarkFailed("  simulated decline  ", "DECLINED", null, CreatedAt.AddMinutes(1));

        Assert.Equal(PaymentTransactionStatus.FAILED, transaction.Status);
        Assert.Equal("simulated decline", transaction.FailureReason);
        Assert.Throws<InvalidOperationException>(() =>
            transaction.MarkSucceeded("SIM-123", null, null, CreatedAt.AddMinutes(2)));
    }

    private static PaymentTransaction CreateTransaction()
    {
        var payment = CreatePayment();
        return PaymentTransaction.Create(Guid.NewGuid(), payment, payment.AmountDue, CreatedAt);
    }

    private static Payment CreatePayment()
        => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 300_000m, "VND", CreatedAt);
}
