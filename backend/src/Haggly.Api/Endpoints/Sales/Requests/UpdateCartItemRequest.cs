namespace Haggly.Api.Endpoints.Sales.Requests;

public sealed record UpdateCartItemRequest(
    decimal Quantity,
    string? Notes);
