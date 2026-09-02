using DomainCart = Haggly.Domain.Modules.Sales.Cart;
using Xunit;

namespace Haggly.UnitTests.Domain.Modules.Sales.Cart;

public sealed class CartItemManagementTests
{
    [Fact]
    public void AddItem_ValidItem_AddsNormalizedItemAndTouchesCart()
    {
        // Arrange
        var cart = CreateCart();

        // Act
        var item = cart.AddItem(InventoryItemId, 2m, "  ripe fruit  ", AddedAt);

        // Assert
        Assert.Equal(InventoryItemId, item.InventoryItemId);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal("ripe fruit", item.Notes);
        Assert.Equal(AddedAt, cart.UpdatedAt);
        Assert.Equal(BuyerId, cart.UpdatedBy);
    }

    [Fact]
    public void AddItem_DuplicateInventoryItem_RejectsWithoutMutation()
    {
        // Arrange
        var cart = CreateCart();
        cart.AddItem(InventoryItemId, 2m, null, AddedAt);

        // Act
        var action = () => cart.AddItem(InventoryItemId, 3m, null, UpdatedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        var item = Assert.Single(cart.Items);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(AddedAt, cart.UpdatedAt);
    }

    [Fact]
    public void UpdateItem_ValidQuantityAndNotes_UpdatesItemAndTimestamp()
    {
        // Arrange
        var cart = CreateCart();
        var item = cart.AddItem(InventoryItemId, 2m, "old", AddedAt);

        // Act
        cart.UpdateItem(item.Id, 4m, "  new note  ", UpdatedAt);

        // Assert
        Assert.Equal(4m, item.Quantity);
        Assert.Equal("new note", item.Notes);
        Assert.Equal(UpdatedAt, item.UpdatedAt);
        Assert.Equal(UpdatedAt, cart.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateItem_InvalidQuantity_RejectsWithoutMutation(decimal quantity)
    {
        // Arrange
        var cart = CreateCart();
        var item = cart.AddItem(InventoryItemId, 2m, "original", AddedAt);

        // Act
        var action = () => cart.UpdateItem(item.Id, quantity, "changed", UpdatedAt);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal("original", item.Notes);
        Assert.Equal(AddedAt, cart.UpdatedAt);
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesAndTouchesCart()
    {
        // Arrange
        var cart = CreateCart();
        var item = cart.AddItem(InventoryItemId, 2m, null, AddedAt);

        // Act
        cart.RemoveItem(item.Id, UpdatedAt);

        // Assert
        Assert.Empty(cart.Items);
        Assert.Equal(UpdatedAt, cart.UpdatedAt);
    }

    [Fact]
    public void RemoveItem_MissingItem_RejectsWithoutMutation()
    {
        // Arrange
        var cart = CreateCart();
        cart.AddItem(InventoryItemId, 2m, null, AddedAt);

        // Act
        var action = () => cart.RemoveItem(Guid.Parse("51000000-0000-0000-0000-000000000099"), UpdatedAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Single(cart.Items);
        Assert.Equal(AddedAt, cart.UpdatedAt);
    }

    private static DomainCart CreateCart() => DomainCart.Create(BuyerId, CreatedAt);

    private static readonly Guid BuyerId = Guid.Parse("51000000-0000-0000-0000-000000000001");
    private static readonly Guid InventoryItemId = Guid.Parse("51000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 30, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AddedAt = new(2026, 8, 30, 3, 1, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 8, 30, 3, 2, 0, TimeSpan.Zero);
}
