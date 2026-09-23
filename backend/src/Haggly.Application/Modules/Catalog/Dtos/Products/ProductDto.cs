using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Modules.Catalog.Dtos.Products;

public sealed record ProductDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    ProductUnit DefaultUnit,
    string? ImageUrl,
    CatalogStatus Status)
{
    public static ProductDto From(Product product)
        => new(
            product.Id,
            product.CategoryId,
            product.Name,
            product.Description,
            product.DefaultUnit,
            product.ImageUrl,
            product.Status);
}
