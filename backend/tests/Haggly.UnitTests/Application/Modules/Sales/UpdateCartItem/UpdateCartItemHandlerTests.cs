using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.UpdateCartItem;

public sealed class UpdateCartItemHandlerTests
{
    private readonly ICartCommandRepository _repository = Substitute.For<ICartCommandRepository>();
    private readonly ICartCatalog _catalog = Substitute.For<ICartCatalog>();
    private readonly ICartQuery _query = Substitute.For<ICartQuery>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidQuantity_UpdatesCartItem()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        var command = new UpdateCartItemCommand(fixture.BuyerId, fixture.Item.Id, 3m, "Updated");

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(fixture.BuyerId, result.BuyerId);
        Assert.Equal(3m, fixture.Item.Quantity);
        Assert.Equal("Updated", fixture.Item.Notes);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CartDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        var fixture = CreateFixture();
        _repository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateCartItemCommand(fixture.BuyerId, fixture.Item.Id, 2m, null),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_InvalidQuantity_ThrowsValidationException(decimal quantity)
    {
        // Arrange
        var fixture = CreateFixture();

        // Act
        var action = () => CreateSubject().Handle(
            new UpdateCartItemCommand(fixture.BuyerId, fixture.Item.Id, quantity, null),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartValidationException>(action);
        await _repository.DidNotReceive().FindByBuyerIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private UpdateCartItemHandler CreateSubject() => new(_repository, _catalog, _query, _clock);

    private void ConfigureFixture(CartFixture fixture)
    {
        _repository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns(fixture.Cart);
        _catalog.GetItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CartItemSnapshot>>([new(
                fixture.Item.InventoryItemId, fixture.ProductStallId, fixture.StallId,
                "Tomato", ProductUnit.KG, 1m, 45_000m, true, 5m, true)]);
        _query.GetAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns((CartReadModel?)null);
        _clock.GetNow().Returns(fixture.Now);
    }

    private static CartFixture CreateFixture()
    {
        var buyerId = Guid.Parse("C1000000-0000-0000-0000-000000000001");
        var inventoryItemId = Guid.Parse("C1000000-0000-0000-0000-000000000002");
        var now = new DateTimeOffset(2026, 8, 30, 9, 30, 0, TimeSpan.Zero);
        var cart = Cart.Create(buyerId, now);
        var item = cart.AddItem(inventoryItemId, 1m, null, now);
        return new CartFixture(
            buyerId, item,
            Guid.Parse("C1000000-0000-0000-0000-000000000003"),
            Guid.Parse("C1000000-0000-0000-0000-000000000004"), cart, now);
    }

    private sealed record CartFixture(Guid BuyerId, CartItem Item, Guid ProductStallId, Guid StallId, Cart Cart, DateTimeOffset Now);
}
