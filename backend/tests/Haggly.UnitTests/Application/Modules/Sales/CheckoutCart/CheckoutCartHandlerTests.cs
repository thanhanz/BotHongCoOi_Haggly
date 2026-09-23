using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.CheckoutCart;

public sealed class CheckoutCartHandlerTests
{
    private readonly ICartCommandRepository _cartRepository = Substitute.For<ICartCommandRepository>();
    private readonly ICartCatalog _catalog = Substitute.For<ICartCatalog>();
    private readonly IOrderCommandRepository _orderRepository = Substitute.For<IOrderCommandRepository>();
    private readonly ICartCheckoutUnitOfWork _unitOfWork = Substitute.For<ICartCheckoutUnitOfWork>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderAndClearsCart()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        var command = new CheckoutCartCommand(fixture.BuyerId);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(fixture.BuyerId, result.BuyerId);
        Assert.Equal(OrderStatus.NEGOTIATING, result.Status);
        Assert.Equal(90_000m, result.TotalToCharge);
        Assert.Empty(fixture.Cart.Items);
        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(order => order.BuyerId == fixture.BuyerId),
            Arg.Any<CancellationToken>());
        await _cartRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyCart_ThrowsValidationWithoutCreatingOrder()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Cart.Clear(fixture.Now);
        _cartRepository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns(fixture.Cart);

        // Act
        var action = () => CreateSubject().Handle(new CheckoutCartCommand(fixture.BuyerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartValidationException>(action);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _cartRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InventoryDroppedBelowCartQuantity_ThrowsValidationWithoutClearing()
    {
        // Arrange
        var fixture = CreateFixture(remainingQuantity: 1m);
        ConfigureFixture(fixture);

        // Act
        var action = () => CreateSubject().Handle(new CheckoutCartCommand(fixture.BuyerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartValidationException>(action);
        Assert.Single(fixture.Cart.Items);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _cartRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ForwardsTokenToCartRepository()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _cartRepository.FindByBuyerIdAsync(fixture.BuyerId, cancellationToken)
            .Returns(Task.FromCanceled<Cart?>(cancellationToken));

        // Act
        var action = () => CreateSubject().Handle(new CheckoutCartCommand(fixture.BuyerId), cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _cartRepository.Received(1).FindByBuyerIdAsync(fixture.BuyerId, cancellationToken);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    private CheckoutCartHandler CreateSubject()
        => new(_cartRepository, _catalog, _orderRepository, _unitOfWork, _clock);

    private void ConfigureFixture(CartFixture fixture)
    {
        _cartRepository.FindByBuyerIdAsync(fixture.BuyerId, Arg.Any<CancellationToken>()).Returns(fixture.Cart);
        _catalog.GetItemsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<CartItemSnapshot>>([new(
                fixture.InventoryItemId, fixture.ProductStallId, fixture.StallId,
                "Tomato", ProductUnit.KG, 1m, 45_000m, true,
                fixture.RemainingQuantity, true)]);
        _clock.GetNow().Returns(fixture.Now);
        _unitOfWork.ExecuteAsync(
                Arg.Any<Func<CancellationToken, Task<Order>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<Order>>>();
                return operation(callInfo.ArgAt<CancellationToken>(1));
            });
    }

    private static CartFixture CreateFixture(decimal remainingQuantity = 5m)
    {
        var buyerId = Guid.Parse("C4000000-0000-0000-0000-000000000001");
        var inventoryItemId = Guid.Parse("C4000000-0000-0000-0000-000000000002");
        var now = new DateTimeOffset(2026, 8, 30, 11, 0, 0, TimeSpan.Zero);
        var cart = Cart.Create(buyerId, now);
        cart.AddItem(inventoryItemId, 2m, null, now);
        return new CartFixture(
            buyerId, inventoryItemId,
            Guid.Parse("C4000000-0000-0000-0000-000000000003"),
            Guid.Parse("C4000000-0000-0000-0000-000000000004"),
            remainingQuantity, cart, now);
    }

    private sealed record CartFixture(Guid BuyerId, Guid InventoryItemId, Guid ProductStallId, Guid StallId, decimal RemainingQuantity, Cart Cart, DateTimeOffset Now);
}
