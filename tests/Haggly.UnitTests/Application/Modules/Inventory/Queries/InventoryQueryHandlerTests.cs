using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Markets;
using NSubstitute;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory.Queries;

public sealed class GetInventoryHandlerTests
{
    private readonly IInventoryQuery _query = Substitute.For<IInventoryQuery>();
    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();

    [Fact]
    public async Task Handle_OwnedActiveStall_ReturnsMappedInventory()
    {
        // Arrange
        var stall = CreateStall();
        var inventory = DomainInventory.Create(StallId, OwnerId, Now);
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(stall);
        _query.GetInventoryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(inventory);

        // Act
        var result = await new GetInventoryHandler(_query, _references).Handle(
            new GetInventoryQuery(StallId, OwnerId), CancellationToken.None);

        // Assert
        Assert.Equal(inventory.Id, result.Id);
        Assert.Equal(StallId, result.StallId);
    }

    [Fact]
    public async Task Handle_MissingInventory_ThrowsNotFound()
    {
        // Arrange
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(CreateStall());
        _query.GetInventoryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DomainInventory?)null);

        // Act
        var action = () => new GetInventoryHandler(_query, _references).Handle(
            new GetInventoryQuery(StallId, OwnerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryNotFoundException>(action);
    }

    [Fact]
    public async Task Handle_InvalidIdentifier_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => new GetInventoryHandler(_query, _references).Handle(
            new GetInventoryQuery(Guid.Empty, OwnerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryValidationException>(action);
        await _references.DidNotReceive().FindActiveStallAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Stall CreateStall() => new() { VendorId = OwnerId, Status = StallStatus.ACTIVE };
    private static readonly Guid StallId = Guid.Parse("97000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("97000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}

public sealed class GetInventoryItemHandlerTests
{
    private readonly IInventoryQuery _query = Substitute.For<IInventoryQuery>();
    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();

    [Fact]
    public async Task Handle_ExistingItem_ReturnsMappedItem()
    {
        // Arrange
        var inventory = DomainInventory.Create(StallId, OwnerId, Now);
        var item = inventory.AddItem(ProductStallId, 5m, OwnerId, Now);
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(CreateStall());
        _query.GetItemAsync(Arg.Any<Guid>(), item.Id, Arg.Any<CancellationToken>()).Returns(item);

        // Act
        var result = await new GetInventoryItemHandler(_query, _references).Handle(
            new GetInventoryItemQuery(StallId, item.Id, OwnerId), CancellationToken.None);

        // Assert
        Assert.Equal(item.Id, result.Id);
        Assert.Equal(5m, result.AvailableQuantity);
    }

    [Fact]
    public async Task Handle_MissingItem_ThrowsNotFound()
    {
        // Arrange
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(CreateStall());
        _query.GetItemAsync(Arg.Any<Guid>(), ItemId, Arg.Any<CancellationToken>()).Returns((InventoryItem?)null);

        // Act
        var action = () => new GetInventoryItemHandler(_query, _references).Handle(
            new GetInventoryItemQuery(StallId, ItemId, OwnerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryNotFoundException>(action);
    }

    [Fact]
    public async Task Handle_InvalidIdentifier_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => new GetInventoryItemHandler(_query, _references).Handle(
            new GetInventoryItemQuery(StallId, Guid.Empty, OwnerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryValidationException>(action);
        await _references.DidNotReceive().FindActiveStallAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Stall CreateStall() => new() { VendorId = OwnerId, Status = StallStatus.ACTIVE };
    private static readonly Guid StallId = Guid.Parse("97100000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("97100000-0000-0000-0000-000000000002");
    private static readonly Guid ProductStallId = Guid.Parse("97100000-0000-0000-0000-000000000003");
    private static readonly Guid ItemId = Guid.Parse("97100000-0000-0000-0000-000000000004");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}

public sealed class GetInventoryLedgerHandlerTests
{
    private readonly IInventoryQuery _query = Substitute.For<IInventoryQuery>();
    private readonly IInventoryReferenceQuery _references = Substitute.For<IInventoryReferenceQuery>();

    [Fact]
    public async Task Handle_ValidFilter_ReturnsMappedPageAndForwardsFilter()
    {
        // Arrange
        var inventory = DomainInventory.Create(StallId, OwnerId, Now);
        var item = inventory.AddItem(ProductStallId, 5m, OwnerId, Now);
        var ledger = Assert.Single(item.InventoryLedgers);
        _references.FindActiveStallAsync(StallId, Arg.Any<CancellationToken>()).Returns(CreateStall());
        _query.GetLedgerAsync(Arg.Any<InventoryLedgerListFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InventoryLedger>([ledger], 2, 10, 11));
        var request = new GetInventoryLedgerQuery(
            StallId, OwnerId, item.Id, InventoryTransactionType.OPENING, 2, 10);

        // Act
        var result = await new GetInventoryLedgerHandler(_query, _references).Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ledger.Id, Assert.Single(result.Items).Id);
        Assert.Equal(11, result.TotalCount);
        await _query.Received(1).GetLedgerAsync(
            Arg.Is<InventoryLedgerListFilter>(filter =>
                filter.StallId != Guid.Empty && filter.InventoryItemId == item.Id
                && filter.Page == 2 && filter.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPaging_ThrowsValidationWithoutQuerying()
    {
        // Arrange
        var request = new GetInventoryLedgerQuery(StallId, OwnerId, null, null, 0, 10);

        // Act
        var action = () => new GetInventoryLedgerHandler(_query, _references).Handle(request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InventoryValidationException>(action);
        await _query.DidNotReceive().GetLedgerAsync(
            Arg.Any<InventoryLedgerListFilter>(), Arg.Any<CancellationToken>());
    }

    private static Stall CreateStall() => new() { VendorId = OwnerId, Status = StallStatus.ACTIVE };
    private static readonly Guid StallId = Guid.Parse("97200000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("97200000-0000-0000-0000-000000000002");
    private static readonly Guid ProductStallId = Guid.Parse("97200000-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
}
