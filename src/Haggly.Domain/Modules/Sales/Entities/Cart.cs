using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Sales;

public sealed class Cart : AuditableEntity
{
    public Guid BuyerId { get; private set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public static Cart Create(Guid buyerId, DateTimeOffset createdAt)
    {
        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("A valid buyer ID is required.", nameof(buyerId));
        }

        return new Cart
        {
            BuyerId = buyerId,
            CreatedAt = createdAt,
            CreatedBy = buyerId
        };
    }

    public CartItem AddItem(
        Guid inventoryItemId,
        decimal quantity,
        string? notes,
        DateTimeOffset occurredAt)
    {
        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("A valid inventory item ID is required.", nameof(inventoryItemId));
        }

        if (Items.Any(item => item.InventoryItemId == inventoryItemId))
        {
            throw new InvalidOperationException("An inventory item can occur only once in a cart.");
        }

        var item = CartItem.Create(Id, inventoryItemId, quantity, notes, occurredAt, BuyerId);
        Items.Add(item);
        Touch(occurredAt);
        return item;
    }

    public void UpdateItem(
        Guid itemId,
        decimal quantity,
        string? notes,
        DateTimeOffset occurredAt)
    {
        var item = Items.SingleOrDefault(value => value.Id == itemId)
            ?? throw new InvalidOperationException("The cart item was not found.");

        item.Update(quantity, notes, occurredAt, BuyerId);
        Touch(occurredAt);
    }

    public void RemoveItem(Guid itemId, DateTimeOffset occurredAt)
    {
        var item = Items.SingleOrDefault(value => value.Id == itemId)
            ?? throw new InvalidOperationException("The cart item was not found.");

        Items.Remove(item);
        Touch(occurredAt);
    }

    public void Clear(DateTimeOffset occurredAt)
    {
        Items.Clear();
        Touch(occurredAt);
    }

    private void Touch(DateTimeOffset occurredAt)
    {
        UpdatedAt = occurredAt;
        UpdatedBy = BuyerId;
    }
}
