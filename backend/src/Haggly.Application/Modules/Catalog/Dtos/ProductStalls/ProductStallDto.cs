using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Modules.Catalog.Dtos.ProductStalls;

public sealed record ProductStallDto(
    Guid Id, 
    Guid StallId, 
    Guid ProductId, 
    string? DisplayName,
    ProductUnit SellingUnit, 
    decimal MinimumOrderQuantity, 
    decimal CurrentUnitPrice,
    bool IsNegotiable, 
    bool IsActive,
    long Version)
{
    public static ProductStallDto From(ProductStall value) 
      => new(
          value.Id, 
          value.StallId, 
          value.ProductId,
          value.DisplayName, 
          value.SellingUnit, 
          value.MinimumOrderQuantity, 
          value.CurrentUnitPrice,
          value.IsNegotiable, 
          value.IsActive,
          value.Version);
}
