using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using MediatR;

namespace Haggly.Application.Modules.Catalog.Queries.ProductStalls;

public sealed record GetProductStallsQuery(Guid StallId, int Page, int PageSize)
    : IRequest<PagedResult<ProductStallDto>>;
