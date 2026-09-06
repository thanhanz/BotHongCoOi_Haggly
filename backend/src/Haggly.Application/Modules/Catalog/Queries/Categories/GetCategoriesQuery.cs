using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Haggly.Application.Common;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.Categories;

public sealed record GetCategoriesQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CategoryDto>>;
