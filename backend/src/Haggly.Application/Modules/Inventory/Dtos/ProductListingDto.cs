using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Modules.Inventory.Dtos;

public sealed record ProductListingDto(
    Guid ProductId,
    Guid ProductStallId,
    Guid InventoryItemId,
    string ProductName,
    string? DisplayName,
    string? ImageUrl,
    Guid StallId,
    string StallName,
    string StallCode,
    decimal CurrentUnitPrice,
    ProductUnit SellingUnit,
    decimal MinimumOrderQuantity,
    decimal AvailableQuantity,
    bool IsNegotiable);
