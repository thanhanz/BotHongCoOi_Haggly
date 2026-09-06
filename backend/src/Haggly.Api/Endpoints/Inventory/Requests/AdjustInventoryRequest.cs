namespace Haggly.Api.Endpoints.Inventory.Requests;

public sealed record AdjustInventoryRequest(
    Guid InventoryItemId,
    decimal QuantityDelta,
    string Reason,
    long ExpectedVersion);
