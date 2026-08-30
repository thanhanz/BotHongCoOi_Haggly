using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentDomainTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidOrderAmountAndCurrency_CreatesPendingPayment()
    {
        var payment = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 300_000m, "vnd", Now);

        Assert.Equal(PaymentStatus.PENDING, payment.Status);
        Assert.Equal(300_000m, payment.AmountDue);
        Assert.Equal(0m, payment.AmountPaid);
        Assert.Equal("VND", payment.Currency);
        Assert.Equal(Now, payment.InitiatedAt);
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_NormalizesStoredTimestampToUtc()
    {
        var localTimestamp = new DateTimeOffset(2026, 8, 22, 8, 0, 0, TimeSpan.FromHours(7));

        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), 300_000m, "VND", localTimestamp);

        Assert.Equal(TimeSpan.Zero, payment.InitiatedAt.Offset);
        Assert.Equal(localTimestamp.UtcDateTime, payment.InitiatedAt.UtcDateTime);
        Assert.Equal(TimeSpan.Zero, payment.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Payment.Create(Guid.NewGuid(), Guid.NewGuid(), amount, "VND", Now));
    }

    [Fact]
    public void StartProcessing_WhenPending_MovesToProcessing()
    {
        var payment = CreatePayment();

        payment.StartProcessing(Now.AddMinutes(1));

        Assert.Equal(PaymentStatus.PROCESSING, payment.Status);
        Assert.Equal(Now.AddMinutes(1), payment.UpdatedAt);
    }

    [Fact]
    public void MarkPaid_WhenProcessing_CollectsCompleteAmount()
    {
        var payment = CreatePayment();
        payment.StartProcessing(Now.AddMinutes(1));

        payment.MarkPaid(Now.AddMinutes(2));

        Assert.Equal(PaymentStatus.PAID, payment.Status);
        Assert.Equal(payment.AmountDue, payment.AmountPaid);
        Assert.Equal(Now.AddMinutes(2), payment.CompletedAt);
    }

    [Fact]
    public void MarkFailed_WhenProcessing_MovesToFailedWithoutCollectingMoney()
    {
        var payment = CreatePayment();
        payment.StartProcessing(Now.AddMinutes(1));

        payment.MarkFailed(Now.AddMinutes(2));

        Assert.Equal(PaymentStatus.FAILED, payment.Status);
        Assert.Equal(0m, payment.AmountPaid);
    }

    [Fact]
    public void StartProcessing_WhenPaid_ThrowsInvalidOperationException()
    {
        var payment = CreatePayment();
        payment.StartProcessing(Now.AddMinutes(1));
        payment.MarkPaid(Now.AddMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            payment.StartProcessing(Now.AddMinutes(3)));
    }

    [Fact]
    public void MarkPaid_WhenFailed_ThrowsInvalidOperationException()
    {
        var payment = CreatePayment();
        payment.StartProcessing(Now.AddMinutes(1));
        payment.MarkFailed(Now.AddMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            payment.MarkPaid(Now.AddMinutes(3)));
    }

    private static Payment CreatePayment()
        => Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 300_000m, "VND", Now);
}
