using Haggly.Domain.Modules.Finance;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Finance.Entities;

public sealed class RevenueLedgerDomainTests
{
    [Fact]
    public void CreatePosSaleSale_WhenAmountIsValid_CreatesNetSaleEntry()
    {
        var saleId = Guid.NewGuid();
        var stallId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 17, 3, 0, 0, TimeSpan.Zero);

        var ledger = RevenueLedger.CreatePosSaleEntry(saleId, stallId, 112_500m, occurredAt);

        Assert.Equal(stallId, ledger.StallId);
        Assert.Equal(saleId, ledger.PosSaleId);
        Assert.Equal(RevenueEntryType.SALE, ledger.EntryType);
        Assert.Equal(112_500m, ledger.GrossAmount);
        Assert.Equal(112_500m, ledger.NetAmount);
        Assert.Equal("POS_SALE", ledger.ReferenceType);
        Assert.Equal(saleId, ledger.ReferenceId);
    }
}
