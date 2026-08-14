using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Modules.Catalog.Dtos.Categories;

public sealed record CategoryDto(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int DisplayOrder,
    CatalogStatus Status)
{
    public static CategoryDto From(Category category)
        => new(
            category.Id,
            category.ParentCategoryId,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.DisplayOrder,
            category.Status);
}
