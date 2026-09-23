using Haggly.Domain.Modules.Finance;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Finance;

public sealed class RevenueLedgerTests
{
    [Fact]
    public void CreatePosSaleEntry_ValidAmount_CreatesNetSaleEntry()
    {
        // Arrange
        var saleId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var stallId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var occurredAt = new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.Zero);

        // Act
        var ledger = RevenueLedger.CreatePosSaleEntry(saleId, stallId, 112_500m, occurredAt);

        // Assert
        Assert.Equal(RevenueEntryType.SALE, ledger.EntryType);
        Assert.Equal(112_500m, ledger.GrossAmount);
        Assert.Equal(112_500m, ledger.NetAmount);
        Assert.Equal("POS_SALE", ledger.ReferenceType);
        Assert.Equal(saleId, ledger.ReferenceId);
    }

    [Fact]
    public void CreatePosSaleEntry_NonPositiveAmount_RejectsEntry()
    {
        // Arrange
        var saleId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var stallId = Guid.Parse("40000000-0000-0000-0000-000000000002");

        // Act
        var action = () => RevenueLedger.CreatePosSaleEntry(
            saleId, stallId, 0m,
            new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CreatePaymentSaleEntry_ValidAllocation_CreatesNetSaleEntry()
    {
        // Arrange
        var allocationId = Guid.Parse("40000000-0000-0000-0000-000000000003");
        var fulfillmentId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        var stallId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var occurredAt = new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.Zero);

        // Act
        var ledger = RevenueLedger.CreatePaymentSaleEntry(
            allocationId, fulfillmentId, stallId, 250_000m, occurredAt);

        // Assert
        Assert.Equal(allocationId, ledger.PaymentAllocationId);
        Assert.Equal(fulfillmentId, ledger.StallFulfillmentId);
        Assert.Equal(250_000m, ledger.NetAmount);
        Assert.Equal("PAYMENT_ALLOCATION", ledger.ReferenceType);
        Assert.Equal(allocationId, ledger.ReferenceId);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void CreatePaymentSaleEntry_MissingAssociation_RejectsEntry(
        bool missingAllocation, bool missingFulfillment, bool missingStall)
    {
        // Arrange
        var allocationId = missingAllocation ? Guid.Empty : Guid.Parse("40000000-0000-0000-0000-000000000003");
        var fulfillmentId = missingFulfillment ? Guid.Empty : Guid.Parse("40000000-0000-0000-0000-000000000004");
        var stallId = missingStall ? Guid.Empty : Guid.Parse("40000000-0000-0000-0000-000000000002");

        // Act
        var action = () => RevenueLedger.CreatePaymentSaleEntry(
            allocationId, fulfillmentId, stallId, 1m,
            new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.Throws<ArgumentException>(action);
    }
}
