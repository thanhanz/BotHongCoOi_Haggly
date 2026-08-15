namespace Haggly.Api.Endpoints.Inventory.Requests;

public sealed record InventoryListingRequest(
    Guid ProductStallId,
    decimal OpeningQuantity,
    decimal? PublicUnitPrice);
