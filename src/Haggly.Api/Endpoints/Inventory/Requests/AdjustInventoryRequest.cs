namespace Haggly.Api.Endpoints.Inventory.Requests;

public sealed record AdjustInventoryRequest(
    Guid ListingId,
    decimal QuantityDelta,
    string Reason,
    long ExpectedVersion);
