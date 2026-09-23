using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.ClearCart;

public sealed class ClearCartHandlerTests
{
    private readonly ICartCommandRepository _repository = Substitute.For<ICartCommandRepository>();
    private readonly ICartQuery _query = Substitute.For<ICartQuery>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_CartHasItems_ClearsAndSavesCart()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);

        // Act
        var result = await CreateSubject().Handle(
            new ClearCartCommand(fixture.BuyerId), CancellationToken.None);

        // Assert
        Assert.Equal(fixture.BuyerId, result.BuyerId);
        Assert.Empty(fixture.Cart.Items);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CartDoesNotExist_ThrowsNotFoundWithoutSaving()
    {
        // Arrange
        var buyerId = Guid.Parse("C3000000-0000-0000-0000-000000000001");
        _repository.FindByBuyerIdAsync(buyerId, Arg.Any<CancellationToken>()).Returns((Cart?)null);

        // Act
        var action = () => CreateSubject().Handle(new ClearCartCommand(buyerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private ClearCartHandler CreateSubject() => new(_repository, _query, _clock);

    private void ConfigureFixture(CartFixture fixture)
    {
        _repository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns(fixture.Cart);
        _query.GetAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns((CartReadModel?)null);
        _clock.GetNow().Returns(fixture.Now);
    }

    private static CartFixture CreateFixture()
    {
        var buyerId = Guid.Parse("C3000000-0000-0000-0000-000000000001");
        var now = new DateTimeOffset(2026, 8, 30, 10, 30, 0, TimeSpan.Zero);
        var cart = Cart.Create(buyerId, now);
        cart.AddItem(Guid.Parse("C3000000-0000-0000-0000-000000000002"), 1m, null, now);
        return new CartFixture(buyerId, cart, now);
    }

    private sealed record CartFixture(Guid BuyerId, Cart Cart, DateTimeOffset Now);
}
