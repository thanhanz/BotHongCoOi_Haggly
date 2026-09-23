using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using DomainPosSale = Haggly.Domain.Modules.Sales.PosSale;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.PosSale;

public sealed class PosSaleCalculationTests
{
    [Fact]
    public void Complete_ItemsWithQuantityAndPrice_CalculatesItemSubtotal()
    {
        // Arrange

        // Act
        var sale = CreateSale([
            new PosSaleItemInput(ItemOneId, "Apple", ProductUnit.KG, 12.345m, 2m)]);

        // Assert
        var item = Assert.Single(sale.Items);
        Assert.Equal(24.69m, item.LineTotal);
    }

    [Fact]
    public void Complete_MultipleItems_CalculatesTotalSaleAmount()
    {
        // Arrange
        var items = new[]
        {
            new PosSaleItemInput(ItemOneId, "Apple", ProductUnit.KG, 12.5m, 2m),
            new PosSaleItemInput(ItemTwoId, "Milk", ProductUnit.PIECE, 7.25m, 3m)
        };

        // Act
        var sale = CreateSale(items);

        // Assert
        Assert.Equal(46.75m, sale.TotalAmount);
        Assert.Equal(sale.TotalAmount, sale.AmountPaid);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    public void Complete_InvalidQuantityOrPrice_RejectsSale(int quantity, decimal price)
    {
        // Arrange
        var item = new PosSaleItemInput(ItemOneId, "Apple", ProductUnit.KG, price, quantity);

        // Act
        var action = () => CreateSale([item]);

        // Assert
        Assert.ThrowsAny<ArgumentOutOfRangeException>(action);
    }

    private static DomainPosSale CreateSale(IReadOnlyCollection<PosSaleItemInput> items) =>
        DomainPosSale.Complete(SaleId, StallId, ActorId, "client-calculation", items, CompletedAt);

    private static readonly Guid SaleId = Guid.Parse("54000000-0000-0000-0000-000000000001");
    private static readonly Guid ItemOneId = Guid.Parse("54000000-0000-0000-0000-000000000002");
    private static readonly Guid ItemTwoId = Guid.Parse("54000000-0000-0000-0000-000000000003");
    private static readonly Guid StallId = Guid.Parse("54000000-0000-0000-0000-000000000004");
    private static readonly Guid ActorId = Guid.Parse("54000000-0000-0000-0000-000000000005");
    private static readonly DateTimeOffset CompletedAt = new(2026, 8, 30, 4, 0, 0, TimeSpan.Zero);
}
