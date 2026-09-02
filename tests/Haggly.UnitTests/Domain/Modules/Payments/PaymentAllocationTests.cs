using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Payments;

public sealed class PaymentAllocationTests
{
    [Fact]
    public void CreateSale_ValidAssociation_CreatesPaymentAllocation()
    {
        // Arrange

        // Act
        var allocation = PaymentAllocation.CreateSale(
            AllocationId, TransactionId, FulfillmentId, StallId, 125_000m, AllocatedAt);

        // Assert
        Assert.Equal(TransactionId, allocation.PaymentTransactionId);
        Assert.Equal(FulfillmentId, allocation.StallFulfillmentId);
        Assert.Equal(StallId, allocation.StallId);
        Assert.Equal(PaymentAllocationType.PAYMENT, allocation.AllocationType);
        Assert.Equal(125_000m, allocation.AllocatedAmount);
        Assert.Equal(AllocatedAt, allocation.AllocatedAt);
        Assert.Equal(AllocatedAt, allocation.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateSale_NonPositiveAmount_RejectsAllocation(decimal amount)
    {
        // Arrange

        // Act
        var action = () => PaymentAllocation.CreateSale(
            AllocationId, TransactionId, FulfillmentId, StallId, amount, AllocatedAt);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreateSale_MissingAssociationId_RejectsAllocation()
    {
        // Arrange

        // Act
        var action = () => PaymentAllocation.CreateSale(
            AllocationId, Guid.Empty, FulfillmentId, StallId, 1m, AllocatedAt);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    private static readonly Guid AllocationId = Guid.Parse("63000000-0000-0000-0000-000000000001");
    private static readonly Guid TransactionId = Guid.Parse("63000000-0000-0000-0000-000000000002");
    private static readonly Guid FulfillmentId = Guid.Parse("63000000-0000-0000-0000-000000000003");
    private static readonly Guid StallId = Guid.Parse("63000000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset AllocatedAt = new(2026, 8, 30, 7, 0, 0, TimeSpan.Zero);
}
