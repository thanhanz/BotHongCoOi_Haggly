using DomainCart = Haggly.Domain.Modules.Sales.Cart;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.Cart;

public sealed class CartCreationTests
{
    [Fact]
    public void Create_ValidBuyer_CreatesEmptyCart()
    {
        // Arrange

        // Act
        var cart = DomainCart.Create(BuyerId, CreatedAt);

        // Assert
        Assert.Equal(BuyerId, cart.BuyerId);
        Assert.Equal(BuyerId, cart.CreatedBy);
        Assert.Equal(CreatedAt, cart.CreatedAt);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Create_EmptyBuyerId_RejectsCart()
    {
        // Arrange

        // Act
        var action = () => DomainCart.Create(Guid.Empty, CreatedAt);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    private static readonly Guid BuyerId = Guid.Parse("51000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 30, 3, 0, 0, TimeSpan.Zero);
}
