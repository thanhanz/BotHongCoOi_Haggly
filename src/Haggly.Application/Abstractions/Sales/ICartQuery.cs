namespace Haggly.Application.Abstractions.Sales;

public interface ICartQuery
{
    Task<CartReadModel?> GetAsync(Guid buyerId, CancellationToken cancellationToken);
}

public sealed record CartReadModel(
    Guid CartId,
    Guid BuyerId,
    IReadOnlyList<CartLineReadModel> Items);

public sealed record CartLineReadModel(
    Guid CartItemId,
    Guid InventoryItemId,
    Guid ProductStallId,
    decimal Quantity,
    string? Notes,
    CartStallReadModel Stall,
    CartProductReadModel Product,
    CartOfferingReadModel Offering,
    decimal RemainingQuantity);

public sealed record CartStallReadModel(
    Guid Id,
    Guid MarketId,
    string Code,
    string Name,
    string? LocationDescription,
    string? PhoneNumber);

public sealed record CartProductReadModel(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    string? ImageUrl);

public sealed record CartOfferingReadModel(
    string? DisplayName,
    Haggly.Domain.Modules.Catalog.ProductUnit SellingUnit,
    decimal MinimumOrderQuantity,
    decimal CurrentUnitPrice,
    bool IsNegotiable);
