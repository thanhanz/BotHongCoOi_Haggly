using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Categories;

public sealed class GetCategoriesHandler(ICategoryQuery query)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    public async Task<IReadOnlyCollection<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await query.GetAllActiveAsync(cancellationToken);

        return categories
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name, StringComparer.Ordinal)
            .Select(CategoryDto.From)
            .ToArray();
    }
}
