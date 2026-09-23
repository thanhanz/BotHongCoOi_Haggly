using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Catalog;

public sealed class Product : SoftDeletableEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductUnit DefaultUnit { get; set; }
    public string? ImageUrl { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.ACTIVE;

    public Category? Category { get; set; }
    public ICollection<ProductStall> ProductStalls { get; set; } = new List<ProductStall>();
}
