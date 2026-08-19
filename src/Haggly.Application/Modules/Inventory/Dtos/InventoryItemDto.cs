using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record InventoryItemDto(
    Guid Id,
    Guid InventoryId,
    Guid ProductStallId,
    decimal CurrentQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    long Version)
{
    public static InventoryItemDto From(InventoryItem value)
        => new(value.Id, value.InventoryId, value.ProductStallId, value.CurrentQuantity,
            value.ReservedQuantity, value.AvailableQuantity, value.Version);
}
