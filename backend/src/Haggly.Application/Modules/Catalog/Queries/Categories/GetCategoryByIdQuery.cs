using Haggly.Application.Modules.Catalog.Dtos.Categories;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.Categories;

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto>;
