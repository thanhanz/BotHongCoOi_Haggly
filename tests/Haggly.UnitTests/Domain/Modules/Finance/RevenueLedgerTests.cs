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
}
