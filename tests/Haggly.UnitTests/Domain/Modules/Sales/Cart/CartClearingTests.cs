using DomainCart = Haggly.Domain.Modules.Sales.Cart;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.Cart;

public sealed class CartClearingTests
{
    [Fact]
    public void Clear_CartWithItems_RemovesAllItemsAndUpdatesTimestamp()
    {
        // Arrange
        var cart = DomainCart.Create(BuyerId, CreatedAt);
        cart.AddItem(InventoryItemId, 2m, null, AddedAt);

        // Act
        cart.Clear(ClearedAt);

        // Assert
        Assert.Empty(cart.Items);
        Assert.Equal(ClearedAt, cart.UpdatedAt);
        Assert.Equal(BuyerId, cart.UpdatedBy);
    }

    [Fact]
    public void Clear_EmptyCart_RemainsEmptyAndUpdatesTimestamp()
    {
        // Arrange
        var cart = DomainCart.Create(BuyerId, CreatedAt);

        // Act
        cart.Clear(ClearedAt);

        // Assert
        Assert.Empty(cart.Items);
        Assert.Equal(ClearedAt, cart.UpdatedAt);
    }

    private static readonly Guid BuyerId = Guid.Parse("52000000-0000-0000-0000-000000000001");
    private static readonly Guid InventoryItemId = Guid.Parse("52000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 30, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AddedAt = new(2026, 8, 30, 3, 1, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ClearedAt = new(2026, 8, 30, 3, 2, 0, TimeSpan.Zero);
}
