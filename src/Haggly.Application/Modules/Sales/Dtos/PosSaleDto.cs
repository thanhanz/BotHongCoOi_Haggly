using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Modules.Sales.Dtos;

public sealed record PosSaleDto(
    Guid Id,
    Guid StallId,
    string SaleNo,
    string ClientRequestId,
    PosSaleStatus Status,
    decimal TotalAmount,
    Guid CompletedBy,
    DateTimeOffset CompletedAt,
    IReadOnlyList<PosSaleItemDto> Items)
{
    public static PosSaleDto From(PosSale sale)
        => new(
            sale.Id,
            sale.StallId,
            sale.SaleNo,
            sale.ClientRequestId,
            sale.Status,
            sale.TotalAmount,
            sale.CompletedBy,
            sale.CompletedAt,
            sale.Items.Select(PosSaleItemDto.From).ToArray());
}

public sealed record PosSaleItemDto(
    Guid Id,
    Guid InventoryItemId,
    string ProductNameSnapshot,
    ProductUnit SellingUnitSnapshot,
    decimal UnitPrice,
    decimal Quantity,
    decimal LineTotal)
{
    public static PosSaleItemDto From(PosSaleItem item)
        => new(
            item.Id,
            item.InventoryItemId,
            item.ProductNameSnapshot,
            item.SellingUnitSnapshot,
            item.UnitPrice,
            item.Quantity,
            item.LineTotal);
}
