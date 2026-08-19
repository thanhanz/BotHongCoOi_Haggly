using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Sales;

public sealed class CartItem : AuditableEntity
{
    public Guid CartId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }

    public Cart? Cart { get; set; }

    internal static CartItem Create(
        Guid cartId,
        Guid inventoryItemId,
        decimal quantity,
        string? notes,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        if (cartId == Guid.Empty || inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("Valid cart and inventory item IDs are required.");
        }

        ValidateQuantity(quantity);
        ValidateNotes(notes);

        return new CartItem
        {
            CartId = cartId,
            InventoryItemId = inventoryItemId,
            Quantity = quantity,
            Notes = NormalizeNotes(notes),
            CreatedAt = occurredAt,
            CreatedBy = actorId
        };
    }

    internal void Update(
        decimal quantity,
        string? notes,
        DateTimeOffset occurredAt,
        Guid actorId)
    {
        ValidateQuantity(quantity);
        ValidateNotes(notes);
        Quantity = quantity;
        Notes = NormalizeNotes(notes);
        UpdatedAt = occurredAt;
        UpdatedBy = actorId;
    }

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Cart quantity must be greater than zero.");
        }
    }

    private static void ValidateNotes(string? notes)
    {
        if (notes?.Length > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(notes), "Cart item notes must not exceed 500 characters.");
        }
    }

    private static string? NormalizeNotes(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
