using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Modules.Sales.Dtos;

namespace Haggly.Application.Modules.Sales.Handlers;

internal static class CartHandlerHelpers
{
    public static async Task<CartDto> ReadAsync(
        ICartQuery query,
        Guid buyerId,
        CancellationToken cancellationToken)
    {
        var value = await query.GetAsync(buyerId, cancellationToken);
        return value is null ? CartDto.Empty(buyerId) : CartDto.From(value);
    }

    public static CartItemSnapshot RequireSnapshot(
        IReadOnlyList<CartItemSnapshot> snapshots,
        Guid inventoryItemId)
    {
        if (snapshots.Count(item => item.InventoryItemId == inventoryItemId) != 1)
        {
            throw new Exceptions.CartValidationException(
                "The inventory item is not available for cart operations.");
        }

        var snapshot = snapshots.Single(item => item.InventoryItemId == inventoryItemId);
        if (!snapshot.IsOrderable)
        {
            throw new Exceptions.CartValidationException(
                "The inventory item is not available for cart operations.");
        }

        return snapshot;
    }

    public static void EnsureQuantity(
        CartItemSnapshot snapshot,
        decimal quantity)
    {
        if (quantity < snapshot.MinimumOrderQuantity)
        {
            throw new Exceptions.CartValidationException(
                "Cart quantity must be at least the product minimum order quantity.");
        }

        if (quantity > snapshot.RemainingQuantity)
        {
            throw new Exceptions.CartValidationException(
                "The requested quantity exceeds remaining inventory quantity.");
        }
    }
}
