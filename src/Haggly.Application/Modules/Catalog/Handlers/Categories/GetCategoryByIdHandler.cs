using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Haggly.Application.Modules.Catalog.Exceptions.Categories;
using Haggly.Application.Modules.Catalog.Queries.Categories;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Categories;

public sealed class GetCategoryByIdHandler(ICategoryQuery query)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
            throw new CategoryValidationException("A valid category ID is required.");

        var category = await query.GetActiveByIdAsync(request.Id, cancellationToken)
            ?? throw new CategoryNotFoundException("The category was not found.");

        return CategoryDto.From(category);
    }
}
