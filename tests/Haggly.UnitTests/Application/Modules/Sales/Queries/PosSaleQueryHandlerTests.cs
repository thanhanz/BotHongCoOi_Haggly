using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.Queries;

public sealed class GetPosSalesHandlerTests
{
    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();
    private readonly IPosSaleQuery _query = Substitute.For<IPosSaleQuery>();

    [Fact]
    public async Task Handle_OwnedActiveStall_ReturnsMappedPage()
    {
        // Arrange
        var stall = CreateStall();
        var sale = CreateSale(stall.Id);
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(stall);
        _query.GetPageAsync(StallId, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<PosSale>([sale], 1, 10, 1));

        // Act
        var result = await new GetPosSalesHandler(_references, _query).Handle(
            new GetPosSalesQuery(StallId, OwnerId, 1, 10), CancellationToken.None);

        // Assert
        Assert.Equal(sale.Id, Assert.Single(result.Items).Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Handle_InvalidPaging_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(CreateStall());

        // Act
        var action = () => new GetPosSalesHandler(_references, _query).Handle(
            new GetPosSalesQuery(StallId, OwnerId, 0, 10), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PosSaleValidationException>(action);
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static Stall CreateStall() => new() { VendorId = OwnerId, Status = StallStatus.ACTIVE };
    private static PosSale CreateSale(Guid stallId) => PosSale.Complete(
        SaleId, stallId, OwnerId, "request-1",
        [new PosSaleItemInput(InventoryItemId, "Apple", ProductUnit.KG, 10m, 2m)], Now);
    private static readonly Guid StallId = Guid.Parse("98300000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("98300000-0000-0000-0000-000000000002");
    private static readonly Guid SaleId = Guid.Parse("98300000-0000-0000-0000-000000000003");
    private static readonly Guid InventoryItemId = Guid.Parse("98300000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}

public sealed class GetPosSaleDetailsHandlerTests
{
    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();
    private readonly IPosSaleQuery _query = Substitute.For<IPosSaleQuery>();

    [Fact]
    public async Task Handle_ExistingSaleInOwnedStall_ReturnsMappedSale()
    {
        // Arrange
        var stall = CreateStall();
        var sale = CreateSale(stall.Id);
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(stall);
        _query.GetByIdWithItemsAsync(StallId, sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        // Act
        var result = await new GetPosSaleDetailsHandler(_references, _query).Handle(
            new GetPosSaleDetailsQuery(StallId, sale.Id, OwnerId), CancellationToken.None);

        // Assert
        Assert.Equal(sale.Id, result.Id);
        Assert.Equal(20m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_MissingSale_ThrowsNotFound()
    {
        // Arrange
        var stall = CreateStall();
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(stall);
        _query.GetByIdWithItemsAsync(StallId, SaleId, Arg.Any<CancellationToken>()).Returns((PosSale?)null);

        // Act
        var action = () => new GetPosSaleDetailsHandler(_references, _query).Handle(
            new GetPosSaleDetailsQuery(StallId, SaleId, OwnerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PosSaleNotFoundException>(action);
    }

    private static Stall CreateStall() => new() { VendorId = OwnerId, Status = StallStatus.ACTIVE };
    private static PosSale CreateSale(Guid stallId) => PosSale.Complete(
        SaleId, stallId, OwnerId, "request-1",
        [new PosSaleItemInput(InventoryItemId, "Apple", ProductUnit.KG, 10m, 2m)], Now);
    private static readonly Guid StallId = Guid.Parse("98400000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("98400000-0000-0000-0000-000000000002");
    private static readonly Guid SaleId = Guid.Parse("98400000-0000-0000-0000-000000000003");
    private static readonly Guid InventoryItemId = Guid.Parse("98400000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}
