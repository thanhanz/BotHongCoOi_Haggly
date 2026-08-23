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

    [Fact]
    public void CreatePaymentSaleEntry_WhenAllocationIsValid_CreatesUtcNetSaleEntry()
    {
        var allocationId = Guid.NewGuid();
        var fulfillmentId = Guid.NewGuid();
        var stallId = Guid.NewGuid();
        var localTime = new DateTimeOffset(2026, 8, 23, 9, 0, 0, TimeSpan.FromHours(7));

        var ledger = RevenueLedger.CreatePaymentSaleEntry(
            allocationId, fulfillmentId, stallId, 120_000m, localTime);

        Assert.Equal(allocationId, ledger.PaymentAllocationId);
        Assert.Equal(fulfillmentId, ledger.StallFulfillmentId);
        Assert.Equal(stallId, ledger.StallId);
        Assert.Equal(RevenueEntryType.SALE, ledger.EntryType);
        Assert.Equal(120_000m, ledger.NetAmount);
        Assert.Equal("PAYMENT_ALLOCATION", ledger.ReferenceType);
        Assert.Equal(allocationId, ledger.ReferenceId);
        Assert.Equal(TimeSpan.Zero, ledger.OccurredAt.Offset);
    }
}
