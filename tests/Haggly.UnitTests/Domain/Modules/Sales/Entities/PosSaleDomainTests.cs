using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.Entities;

public sealed class PosSaleDomainTests
{
    private static readonly DateTimeOffset CompletedAt =
        new(2026, 8, 15, 3, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Complete_WhenItemsAreValid_CreatesCompletedSaleAndCalculatesTotal()
    {
        var saleId = Guid.NewGuid();
        var stallId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        var sale = PosSale.Complete(
            saleId,
            stallId,
            actorId,
            "client-001",
            [new PosSaleItemInput(
                listingId,
                "Tomato",
                ProductUnit.KG,
                45_000m,
                2.5m)],
            CompletedAt);

        Assert.Equal(saleId, sale.Id);
        Assert.Equal(stallId, sale.StallId);
        Assert.Equal(actorId, sale.CompletedBy);
        Assert.Equal("client-001", sale.ClientRequestId);
        Assert.Equal(PosSaleStatus.COMPLETED, sale.Status);
        Assert.Equal(112_500m, sale.TotalAmount);
        Assert.Equal(CompletedAt, sale.CompletedAt);
        var item = Assert.Single(sale.Items);
        Assert.Equal(listingId, item.DailyProductListingId);
        Assert.Equal(112_500m, item.LineTotal);
    }

    [Fact]
    public void Complete_WhenItemsAreEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PosSale.Complete(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "client-001",
            [],
            CompletedAt));
    }

    [Fact]
    public void Complete_WhenDuplicateListingIdsAreProvided_ThrowsArgumentException()
    {
        var listingId = Guid.NewGuid();
        var item = new PosSaleItemInput(
            listingId,
            "Tomato",
            ProductUnit.KG,
            45_000m,
            1m);

        Assert.Throws<ArgumentException>(() => PosSale.Complete(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "client-001",
            [item, item],
            CompletedAt));
    }
}
