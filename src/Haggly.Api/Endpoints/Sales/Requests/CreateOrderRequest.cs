namespace Haggly.Api.Endpoints.Sales.Requests;

public sealed record CreateOrderRequest(
    IReadOnlyCollection<CreateOrderItemRequest>? Items);

public sealed record CreateOrderItemRequest(
    Guid InventoryItemId,
    decimal Quantity,
    string? Notes);
