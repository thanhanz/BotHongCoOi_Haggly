using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Exceptions;
using MediatR;

namespace Haggly.Application.Modules.Inventory.Queries;

public sealed class GetProductListingsHandler(IProductListingReader reader)
    : IRequestHandler<GetProductListingsQuery, PagedResult<ProductListingDto>>
{
    public Task<PagedResult<ProductListingDto>> Handle(
        GetProductListingsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty)
            throw new InventoryValidationException("A valid category ID is required.");

        if (request.StallId == Guid.Empty)
            throw new InventoryValidationException("A valid stall ID is required.");

        if (request.Page < 1)
            throw new InventoryValidationException("Page must be at least 1.");

        if (request.PageSize is < 1 or > 100)
            throw new InventoryValidationException("Page size must be between 1 and 100.");

        var sort = string.IsNullOrWhiteSpace(request.Sort) ? "home" : request.Sort.Trim().ToLowerInvariant();
        if (sort != "home")
            throw new InventoryValidationException("Sort must be 'home' when provided.");

        return reader.GetPageAsync(
            new ProductListingListFilter(request.CategoryId, request.StallId, sort, request.Page, request.PageSize),
            cancellationToken);
    }
}
