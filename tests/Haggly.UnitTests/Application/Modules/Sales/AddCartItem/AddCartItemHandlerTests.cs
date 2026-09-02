using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.AddCartItem;

public sealed class AddCartItemHandlerTests
{
    private readonly ICartCommandRepository _repository = Substitute.For<ICartCommandRepository>();
    private readonly ICartCatalog _catalog = Substitute.For<ICartCatalog>();
    private readonly ICartQuery _query = Substitute.For<ICartQuery>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidQuantity_CreatesAndSavesBuyerCart()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureSnapshot(fixture);
        _repository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>())
            .Returns((Cart?)null);
        _query.GetAsync(fixture.BuyerId, Arg.Any<CancellationToken>())
            .Returns((CartReadModel?)null);
        var command = new AddCartItemCommand(fixture.BuyerId, fixture.InventoryItemId, 2m, "Ripe");

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(fixture.BuyerId, result.BuyerId);
        await _repository.Received(1).AddAsync(
            Arg.Is<Cart>(cart => cart.BuyerId == fixture.BuyerId && cart.Items.Count == 1),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_QuantityExceedsRemaining_ThrowsValidationException()
    {
        // Arrange
        var fixture = CreateFixture(remainingQuantity: 1m);
        ConfigureSnapshot(fixture);
        var command = new AddCartItemCommand(fixture.BuyerId, fixture.InventoryItemId, 2m, null);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartValidationException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCartItem_ThrowsConflictWithoutSaving()
    {
        // Arrange
        var fixture = CreateFixture();
        var cart = Cart.Create(fixture.BuyerId, fixture.Now);
        cart.AddItem(fixture.InventoryItemId, 1m, null, fixture.Now);
        ConfigureSnapshot(fixture);
        _repository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns(cart);
        _query.GetAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns((CartReadModel?)null);

        // Act
        var action = () => CreateSubject().Handle(
            new AddCartItemCommand(fixture.BuyerId, fixture.InventoryItemId, 2m, null),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartConflictException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ForwardsTokenToCatalog()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _catalog.GetItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), cancellationToken)
            .Returns(Task.FromCanceled<IReadOnlyList<CartItemSnapshot>>(cancellationToken));

        // Act
        var action = () => CreateSubject().Handle(
            new AddCartItemCommand(fixture.BuyerId, fixture.InventoryItemId, 1m, null),
            cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _catalog.Received(1).GetItemsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { fixture.InventoryItemId })),
            cancellationToken);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private AddCartItemHandler CreateSubject() => new(_repository, _catalog, _query, _clock);

    private void ConfigureSnapshot(CartFixture fixture)
    {
        _catalog.GetItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CartItemSnapshot>>([new(
                fixture.InventoryItemId, fixture.ProductStallId, fixture.StallId,
                "Tomato", ProductUnit.KG, 1m, 45_000m, true,
                fixture.RemainingQuantity, true)]);
        _clock.GetNow().Returns(fixture.Now);
    }

    private static CartFixture CreateFixture(decimal remainingQuantity = 5m)
        => new(
            Guid.Parse("C0000000-0000-0000-0000-000000000001"),
            Guid.Parse("C0000000-0000-0000-0000-000000000002"),
            Guid.Parse("C0000000-0000-0000-0000-000000000003"),
            Guid.Parse("C0000000-0000-0000-0000-000000000004"),
            remainingQuantity,
            new DateTimeOffset(2026, 8, 30, 9, 0, 0, TimeSpan.Zero));

    private sealed record CartFixture(Guid BuyerId, Guid InventoryItemId, Guid ProductStallId, Guid StallId, decimal RemainingQuantity, DateTimeOffset Now);
}
