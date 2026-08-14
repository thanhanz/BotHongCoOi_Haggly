using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.ProductStalls;

public sealed class GetProductStallByIdHandler(IProductStallQuery query)
    : IRequestHandler<GetProductStallByIdQuery, ProductStallDto>
{
    public async Task<ProductStallDto> Handle(GetProductStallByIdQuery c, CancellationToken ct)
    {
        if (c.StallId == Guid.Empty || c.Id == Guid.Empty)
            throw new ProductStallValidationException("Valid stall and product-stall IDs are required.");
        
        return ProductStallDto.From(await query.GetActiveByIdAsync(c.StallId, c.Id, ct)
            ?? throw new ProductStallNotFoundException("The product was not found in this stall."));
    }
}
