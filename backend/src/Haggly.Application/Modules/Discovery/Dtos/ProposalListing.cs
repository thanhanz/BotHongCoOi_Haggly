namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record ProposalListing(
    Guid ProductId,
    string ProductName,
    string DisplayName,
    Guid InventoryItemId,
    Guid StallId,
    string StallName,
    string SellingUnit,
    decimal MinimumOrderQuantity,
    decimal CurrentUnitPrice,
    decimal AvailableQuantity);
