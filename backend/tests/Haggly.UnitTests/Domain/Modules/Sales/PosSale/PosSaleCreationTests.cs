using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using DomainPosSale = Haggly.Domain.Modules.Sales.PosSale;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.PosSale;

public sealed class PosSaleCreationTests
{
    [Fact]
    public void Complete_ValidItems_CreatesCompletedSaleWithIdempotencyData()
    {
        // Arrange
        var saleId = Guid.Parse("53000000-0000-0000-0000-000000000001");
        var inventoryItemId = Guid.Parse("53000000-0000-0000-0000-000000000002");

        // Act
        var sale = DomainPosSale.Complete(
            saleId, StallId, ActorId, "  client-001  ",
            [new PosSaleItemInput(inventoryItemId, "Apple", ProductUnit.KG, 12.5m, 2m)],
            CompletedAt);

        // Assert
        Assert.Equal(saleId, sale.Id);
        Assert.Equal("client-001", sale.ClientRequestId);
        Assert.Equal(PosSaleStatus.COMPLETED, sale.Status);
        Assert.Equal(PaymentStatus.PAID, sale.PaymentStatus);
        Assert.Equal(25m, sale.TotalAmount);
        Assert.Equal(25m, sale.AmountPaid);
        Assert.Equal(ActorId, sale.CompletedBy);
    }

    [Fact]
    public void Complete_EmptyItems_RejectsSale()
    {
        // Arrange

        // Act
        var action = () => DomainPosSale.Complete(
            SaleId, StallId, ActorId, "client-002", [], CompletedAt);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Complete_DuplicateInventoryItems_RejectsSaleWithoutReturningPartialAggregate()
    {
        // Arrange
        var input = new PosSaleItemInput(InventoryItemId, "Apple", ProductUnit.KG, 12.5m, 1m);

        // Act
        var action = () => DomainPosSale.Complete(
            SaleId, StallId, ActorId, "client-003", [input, input], CompletedAt);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    private static readonly Guid SaleId = Guid.Parse("53000000-0000-0000-0000-000000000001");
    private static readonly Guid StallId = Guid.Parse("53000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("53000000-0000-0000-0000-000000000004");
    private static readonly Guid InventoryItemId = Guid.Parse("53000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset CompletedAt = new(2026, 8, 30, 4, 0, 0, TimeSpan.Zero);
}
