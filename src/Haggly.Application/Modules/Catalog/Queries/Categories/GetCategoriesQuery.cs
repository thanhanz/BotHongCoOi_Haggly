using Haggly.Application.Modules.Catalog.Dtos.Categories;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.Categories;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyCollection<CategoryDto>>;
