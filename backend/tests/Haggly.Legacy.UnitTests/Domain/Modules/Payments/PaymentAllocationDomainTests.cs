using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentAllocationDomainTests
{
    [Fact]
    public void CreateSale_WhenInputIsValid_CreatesUtcImmutableAllocation()
    {
        var transactionId = Guid.NewGuid();
        var fulfillmentId = Guid.NewGuid();
        var stallId = Guid.NewGuid();
        var localTime = new DateTimeOffset(2026, 8, 23, 9, 0, 0, TimeSpan.FromHours(7));

        var allocation = PaymentAllocation.CreateSale(
            Guid.NewGuid(), transactionId, fulfillmentId, stallId, 120_000m, localTime);

        Assert.Equal(transactionId, allocation.PaymentTransactionId);
        Assert.Equal(fulfillmentId, allocation.StallFulfillmentId);
        Assert.Equal(stallId, allocation.StallId);
        Assert.Equal(PaymentAllocationType.PAYMENT, allocation.AllocationType);
        Assert.Equal(120_000m, allocation.AllocatedAmount);
        Assert.Equal(TimeSpan.Zero, allocation.AllocatedAt.Offset);
    }

    [Fact]
    public void CreateSale_WhenAmountIsNotPositive_ThrowsArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => PaymentAllocation.CreateSale(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m,
            DateTimeOffset.UtcNow));
}
