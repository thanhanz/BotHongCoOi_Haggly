using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using DomainPosSale = Haggly.Domain.Modules.Sales.PosSale;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.PosSale;

public sealed class PosSaleCompletionTests
{
    [Fact]
    public void Complete_WithExplicitAmountPaid_CompletesSaleOnceWithPaidStatus()
    {
        // Arrange
        var total = 30m;

        // Act
        var sale = DomainPosSale.Complete(
            SaleId, StallId, ActorId, "client-completion",
            [new PosSaleItemInput(ItemId, "Apple", ProductUnit.KG, total, 1m)],
            CompletedAt, PaymentMethodCode.CASH, total);

        // Assert
        Assert.Equal(PosSaleStatus.COMPLETED, sale.Status);
        Assert.Equal(PaymentStatus.PAID, sale.PaymentStatus);
        Assert.Single(sale.Items);
        Assert.Equal("client-completion", sale.ClientRequestId);
    }

    [Fact]
    public void Complete_AmountPaidDoesNotMatchTotal_RejectsSale()
    {
        // Arrange

        // Act
        var action = () => DomainPosSale.Complete(
            SaleId, StallId, ActorId, "client-invalid-payment",
            [new PosSaleItemInput(ItemId, "Apple", ProductUnit.KG, 30m, 1m)],
            CompletedAt, PaymentMethodCode.CASH, 29m);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    private static readonly Guid SaleId = Guid.Parse("55000000-0000-0000-0000-000000000001");
    private static readonly Guid ItemId = Guid.Parse("55000000-0000-0000-0000-000000000002");
    private static readonly Guid StallId = Guid.Parse("55000000-0000-0000-0000-000000000003");
    private static readonly Guid ActorId = Guid.Parse("55000000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset CompletedAt = new(2026, 8, 30, 4, 0, 0, TimeSpan.Zero);
}
