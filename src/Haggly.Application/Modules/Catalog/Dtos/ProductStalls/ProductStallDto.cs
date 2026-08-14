using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Modules.Catalog.Dtos.ProductStalls;

public sealed record ProductStallDto(
    Guid Id, 
    Guid StallId, 
    Guid ProductId, 
    string? DisplayName,
    ProductUnit SellingUnit, 
    decimal MinimumOrderQuantity, 
    decimal DefaultUnitPrice,
    bool IsNegotiable, 
    bool IsActive)
{
    public static ProductStallDto From(ProductStall value) 
      => new(
          value.Id, 
          value.StallId, 
          value.ProductId,
          value.DisplayName, 
          value.SellingUnit, 
          value.MinimumOrderQuantity, 
          value.DefaultUnitPrice,
          value.IsNegotiable, 
          value.IsActive);
}
