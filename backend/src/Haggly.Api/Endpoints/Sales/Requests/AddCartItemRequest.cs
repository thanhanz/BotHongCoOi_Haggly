namespace Haggly.Api.Endpoints.Sales.Requests;

public sealed record AddCartItemRequest(
    Guid InventoryItemId,
    decimal Quantity,
    string? Notes);
