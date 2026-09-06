namespace Haggly.Api.Endpoints.Inventory.Requests;

public sealed record AddInventoryItemRequest(Guid ProductStallId, decimal CurrentQuantity);
