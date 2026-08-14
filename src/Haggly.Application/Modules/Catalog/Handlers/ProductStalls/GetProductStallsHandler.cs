using Haggly.Application.Abstractions.Catalog;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Application.Modules.Catalog.Exceptions.ProductStalls;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Handlers.ProductStalls;

public sealed class GetProductStallsHandler(IProductStallQuery query)
    : IRequestHandler<GetProductStallsQuery, PagedResult<ProductStallDto>>
{
    public async Task<PagedResult<ProductStallDto>> Handle(GetProductStallsQuery c, CancellationToken ct)
    {
        if (c.StallId == Guid.Empty || c.Page < 1 || c.PageSize is < 1 or > 100)
            throw new ProductStallValidationException("Valid stall, page, and page size are required.");
        
        var result = await query.GetProductsStallAsync(new ProductStallListFilter(c.StallId, c.Page, c.PageSize), ct);
        
        return new(result.Items.Select(ProductStallDto.From).ToArray(), result.Page, result.PageSize, result.TotalCount);
    }
}
