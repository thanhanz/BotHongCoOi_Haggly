using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Modules.Sales.Dtos;

public sealed record OrderDto(
    Guid Id,
    string OrderNo,
    Guid BuyerId,
    OrderStatus Status,
    decimal TotalToCharge,
    decimal TotalPaid,
    string Currency,
    DateTimeOffset? PlacedAt,
    DateTimeOffset? PaymentDueAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<OrderFulfillmentDto> Fulfillments)
{
    public static OrderDto From(Order order)
        => new(
            order.Id,
            order.OrderNo,
            order.BuyerId,
            order.Status,
            order.TotalToCharge,
            order.TotalPaid,
            order.Currency,
            order.PlacedAt,
            order.PaymentDueAt,
            order.CancelledAt,
            order.CancellationReason,
            order.StallFulfillments.Select(OrderFulfillmentDto.From).ToArray());
}

public sealed record OrderFulfillmentDto(
    Guid Id,
    Guid StallId,
    string FulfillmentNo,
    StallFulfillmentStatus Status,
    decimal Subtotal,
    decimal FinalAmount,
    decimal PaidAmount,
    DateTimeOffset? PreparedAt,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<OrderItemDto> Items)
{
    public static OrderFulfillmentDto From(StallFulfillment fulfillment)
        => new(
            fulfillment.Id,
            fulfillment.StallId,
            fulfillment.FulfillmentNo,
            fulfillment.Status,
            fulfillment.Subtotal,
            fulfillment.FinalAmount,
            fulfillment.PaidAmount,
            fulfillment.PreparedAt,
            fulfillment.ReadyAt,
            fulfillment.PickedUpAt,
            fulfillment.CancelledAt,
            fulfillment.CancellationReason,
            fulfillment.OrderItems.Select(OrderItemDto.From).ToArray());
}

public sealed record OrderItemDto(
    Guid Id,
    Guid InventoryItemId,
    string ProductNameSnapshot,
    string SellingUnitSnapshot,
    decimal PublicUnitPriceSnapshot,
    decimal FinalUnitPrice,
    decimal FinalQuantity,
    decimal LineTotal,
    bool IsNegotiated,
    OrderItemStatus Status,
    string? Notes)
{
    public static OrderItemDto From(OrderItem item)
        => new(
            item.Id,
            item.InventoryItemId,
            item.ProductNameSnapshot,
            item.SellingUnitSnapshot,
            item.PublicUnitPriceSnapshot,
            item.FinalUnitPrice,
            item.FinalQuantity,
            item.LineTotal,
            item.IsNegotiated,
            item.Status,
            item.Notes);
}
