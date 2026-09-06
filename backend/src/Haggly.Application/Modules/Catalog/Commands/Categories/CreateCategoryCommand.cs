using Haggly.Application.Modules.Catalog.Dtos.Categories;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Commands.Categories;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentCategoryId,
    int DisplayOrder) : IRequest<CategoryDto>;
