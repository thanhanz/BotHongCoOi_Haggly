using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Products;

public sealed class GetProductsHandler(IProductQuery query)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty)
            throw new ProductValidationException("A valid category ID is required.");

        if (request.Page < 1)
            throw new ProductValidationException("Page must be at least 1.");

        if (request.PageSize is < 1 or > 100)
            throw new ProductValidationException("Page size must be between 1 and 100.");

        var products = await query.GetPageAsync(
            new ProductListFilter(request.CategoryId, request.Page, request.PageSize),
            cancellationToken);

        return new PagedResult<ProductDto>(
            products.Items.Select(ProductDto.From).ToArray(),
            products.Page,
            products.PageSize,
            products.TotalCount);
    }
}
