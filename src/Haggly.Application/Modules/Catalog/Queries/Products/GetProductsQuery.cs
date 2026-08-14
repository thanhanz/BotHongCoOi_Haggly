using Haggly.Application.Modules.Catalog.Dtos.Products;
using Haggly.Application.Common;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.Products;

public sealed record GetProductsQuery(
    Guid? CategoryId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
