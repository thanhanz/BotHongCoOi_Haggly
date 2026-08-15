namespace Haggly.Application.Modules.Inventory.Commands;


// A record representing the request to add a daily product listing (in Presentation layer)
public sealed record InventoryListingInput(
    Guid ProductStallId,
    decimal OpeningQuantity,
    decimal? PublicUnitPrice);
