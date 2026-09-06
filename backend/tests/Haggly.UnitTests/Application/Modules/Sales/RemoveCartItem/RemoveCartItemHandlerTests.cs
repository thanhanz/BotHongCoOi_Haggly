using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.RemoveCartItem;

public sealed class RemoveCartItemHandlerTests
{
    private readonly ICartCommandRepository _repository = Substitute.For<ICartCommandRepository>();
    private readonly ICartQuery _query = Substitute.For<ICartQuery>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ExistingCartItem_RemovesItemAndSaves()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);

        // Act
        var result = await CreateSubject().Handle(
            new RemoveCartItemCommand(fixture.BuyerId, fixture.Item.Id), CancellationToken.None);

        // Assert
        Assert.Equal(fixture.BuyerId, result.BuyerId);
        Assert.Empty(fixture.Cart.Items);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);

        // Act
        var action = () => CreateSubject().Handle(
            new RemoveCartItemCommand(fixture.BuyerId, Guid.Parse("C2000000-0000-0000-0000-000000000009")),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private RemoveCartItemHandler CreateSubject() => new(_repository, _query, _clock);

    private void ConfigureFixture(CartFixture fixture)
    {
        _repository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns(fixture.Cart);
        _query.GetAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns((CartReadModel?)null);
        _clock.GetNow().Returns(fixture.Now);
    }

    private static CartFixture CreateFixture()
    {
        var buyerId = Guid.Parse("C2000000-0000-0000-0000-000000000001");
        var now = new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero);
        var cart = Cart.Create(buyerId, now);
        var item = cart.AddItem(Guid.Parse("C2000000-0000-0000-0000-000000000002"), 1m, null, now);
        return new CartFixture(buyerId, cart, item, now);
    }

    private sealed record CartFixture(Guid BuyerId, Cart Cart, CartItem Item, DateTimeOffset Now);
}
