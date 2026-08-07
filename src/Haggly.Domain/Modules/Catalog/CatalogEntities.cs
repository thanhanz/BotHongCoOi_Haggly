using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Catalog;

public sealed class Category : SoftDeletableEntity
{
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;

    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public sealed class Product : SoftDeletableEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductUnit DefaultUnit { get; set; }
    public string? ImageUrl { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;

    public Category? Category { get; set; }
    public ICollection<ProductStall> ProductStalls { get; set; } = new List<ProductStall>();
}

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
