using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Application.Modules.Catalog.Exceptions.Products;
using Haggly.Application.Modules.Catalog.Queries.Products;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.Products;

public sealed class GetProductByIdHandler(IProductQuery query)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
            throw new ProductValidationException("A valid product ID is required.");

        var product = await query.GetActiveByIdAsync(request.Id, cancellationToken)
            ?? throw new ProductNotFoundException("The product was not found.");

        return ProductDto.From(product);
    }
}
