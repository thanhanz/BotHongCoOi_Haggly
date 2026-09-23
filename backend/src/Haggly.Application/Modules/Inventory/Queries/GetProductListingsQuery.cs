using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed record GetProductListingsQuery(
    Guid? CategoryId,
    Guid? StallId,
    string? Sort,
    int Page,
    int PageSize) : IRequest<PagedResult<ProductListingDto>>;
