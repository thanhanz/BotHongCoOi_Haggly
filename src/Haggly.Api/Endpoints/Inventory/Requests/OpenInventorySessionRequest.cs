namespace Haggly.Api.Endpoints.Inventory.Requests;

public sealed record OpenInventorySessionRequest(
    string? Notes,
    IReadOnlyCollection<InventoryListingRequest>? Listings);
