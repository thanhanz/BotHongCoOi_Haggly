using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Products;

public sealed class GetProductsHandler(IProductQuery query)
    : IRequestHandler<GetProductsQuery, IReadOnlyCollection<ProductDto>>
{
    public async Task<IReadOnlyCollection<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty)
            throw new ProductValidationException("A valid category ID is required.");

        var products = await query.GetAllActiveAsync(request.CategoryId, cancellationToken);

        return products
            .OrderBy(product => product.Name, StringComparer.Ordinal)
            .Select(ProductDto.From)
            .ToArray();
    }
}
