using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Categories;

public sealed class GetCategoriesHandler(ICategoryQuery query)
    : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    public async Task<PagedResult<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1)
            throw new CategoryValidationException("Page must be at least 1.");

        if (request.PageSize is < 1 or > 100)
            throw new CategoryValidationException("Page size must be between 1 and 100.");

        var categories = await query.GetPageAsync(
            new CategoryListFilter(request.Page, request.PageSize),
            cancellationToken);

        return new PagedResult<CategoryDto>(
            categories.Items.Select(CategoryDto.From).ToArray(),
            categories.Page,
            categories.PageSize,
            categories.TotalCount);
    }
}
