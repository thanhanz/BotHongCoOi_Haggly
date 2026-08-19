using Haggly.Application.Abstractions.Sales;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Modules.Sales.Dtos;

public sealed record CartDto(
    Guid Id,
    Guid BuyerId,
    int ItemCount,
    decimal Subtotal,
    IReadOnlyList<CartStallDto> Stalls)
{
    public static CartDto Empty(Guid buyerId)
        => new(Guid.Empty, buyerId, 0, 0m, []);

    public static CartDto From(CartReadModel value)
    {
        var stalls = value.Items
            .GroupBy(item => item.Stall.Id)
            .Select(group => new CartStallDto(
                group.First().Stall,
                Round(group.Sum(item => item.Quantity * item.Offering.CurrentUnitPrice)),
                group.Select(CartItemDto.From).ToArray()))
            .ToArray();

        return new CartDto(
            value.CartId,
            value.BuyerId,
            value.Items.Count,
            Round(value.Items.Sum(item => item.Quantity * item.Offering.CurrentUnitPrice)),
            stalls);
    }

    private static decimal Round(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record CartStallDto(
    CartStallReadModel Stall,
    decimal Subtotal,
    IReadOnlyList<CartItemDto> Items);

public sealed record CartItemDto(
    Guid CartItemId,
    Guid InventoryItemId,
    Guid ProductStallId,
    decimal Quantity,
    string? Notes,
    CartProductReadModel Product,
    CartOfferingDto Offering,
    decimal RemainingQuantity,
    bool IsQuantityAvailable,
    decimal LineTotal)
{
    public static CartItemDto From(CartLineReadModel value)
        => new(
            value.CartItemId,
            value.InventoryItemId,
            value.ProductStallId,
            value.Quantity,
            value.Notes,
            value.Product,
            CartOfferingDto.From(value.Offering),
            value.RemainingQuantity,
            value.Quantity <= value.RemainingQuantity,
            decimal.Round(
                value.Quantity * value.Offering.CurrentUnitPrice,
                2,
                MidpointRounding.AwayFromZero));
}

public sealed record CartOfferingDto(
    string? DisplayName,
    ProductUnit SellingUnit,
    decimal MinimumOrderQuantity,
    decimal CurrentUnitPrice,
    bool IsNegotiable)
{
    public static CartOfferingDto From(CartOfferingReadModel value)
        => new(
            value.DisplayName,
            value.SellingUnit,
            value.MinimumOrderQuantity,
            value.CurrentUnitPrice,
            value.IsNegotiable);
}
