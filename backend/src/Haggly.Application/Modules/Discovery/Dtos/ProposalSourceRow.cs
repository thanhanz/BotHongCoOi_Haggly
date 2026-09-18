namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record ProposalSourceRow(
    Guid DishId,
    string DishName,
    Guid CanonicalIngredientId,
    string Code,
    string IngredientName,
    Guid? ProductId,
    string? ProductName,
    bool ProductActive,
    Guid? ProductStallId,
    string? DisplayName,
    string? SellingUnit,
    decimal? MinimumOrderQuantity,
    decimal? CurrentUnitPrice,
    bool ListingActive,
    Guid? InventoryItemId,
    decimal? CurrentQuantity,
    decimal? ReservedQuantity,
    Guid? StallId,
    string? StallName,
    bool StallActive,
    bool MarketActive);
