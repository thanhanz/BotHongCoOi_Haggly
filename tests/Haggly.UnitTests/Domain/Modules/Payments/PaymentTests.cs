using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentTests
{
    [Fact]
    public void Create_ValidAmountAndCurrency_CreatesPendingPayment()
    {
        // Arrange
        var initiatedAt = new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

        // Act
        var payment = Payment.Create(
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Guid.Parse("30000000-0000-0000-0000-000000000002"),
            300_000m, "vnd", initiatedAt);

        // Assert
        Assert.Equal(PaymentStatus.PENDING, payment.Status);
        Assert.Equal(300_000m, payment.AmountDue);
        Assert.Equal("VND", payment.Currency);
        Assert.Equal(initiatedAt, payment.InitiatedAt);
    }

    [Fact]
    public void StartProcessing_PendingPayment_MovesToProcessing()
    {
        // Arrange
        var payment = CreatePayment();
        var occurredAt = new DateTimeOffset(2026, 8, 21, 12, 1, 0, TimeSpan.Zero);

        // Act
        payment.StartProcessing(occurredAt);

        // Assert
        Assert.Equal(PaymentStatus.PROCESSING, payment.Status);
        Assert.Equal(occurredAt, payment.UpdatedAt);
    }

    [Fact]
    public void MarkPaid_ProcessingPayment_CollectsAmountDue()
    {
        // Arrange
        var payment = CreatePayment();
        payment.StartProcessing(new DateTimeOffset(2026, 8, 21, 12, 1, 0, TimeSpan.Zero));
        var occurredAt = new DateTimeOffset(2026, 8, 21, 12, 2, 0, TimeSpan.Zero);

        // Act
        payment.MarkPaid(occurredAt);

        // Assert
        Assert.Equal(PaymentStatus.PAID, payment.Status);
        Assert.Equal(payment.AmountDue, payment.AmountPaid);
        Assert.Equal(occurredAt, payment.CompletedAt);
    }

    [Fact]
    public void MarkPaid_FailedPayment_RejectsInvalidTransition()
    {
        // Arrange
        var payment = CreatePayment();
        payment.StartProcessing(new DateTimeOffset(2026, 8, 21, 12, 1, 0, TimeSpan.Zero));
        payment.MarkFailed(new DateTimeOffset(2026, 8, 21, 12, 2, 0, TimeSpan.Zero));

        // Act
        var action = () => payment.MarkPaid(new DateTimeOffset(2026, 8, 21, 12, 3, 0, TimeSpan.Zero));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    private static Payment CreatePayment()
        => Payment.Create(
            Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Guid.Parse("30000000-0000-0000-0000-000000000002"),
            300_000m, "VND",
            new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero));
}
