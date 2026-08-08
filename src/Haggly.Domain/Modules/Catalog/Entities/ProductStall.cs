using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Catalog;

public sealed class ProductStall : SoftDeletableEntity
{
    public Guid StallId { get; set; }
    public Guid ProductId { get; set; }
    public string? DisplayName { get; set; }
    public ProductUnit SellingUnit { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public decimal DefaultUnitPrice { get; set; }
    public bool IsNegotiable { get; set; }
    public bool IsActive { get; set; } = true;

    public Stall? Stall { get; set; }
    public Product? Product { get; set; }
}
