using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Queries;

namespace Haggly.Application.Abstractions.Inventory;

public interface IProductListingReader
{
    Task<PagedResult<ProductListingDto>> GetPageAsync(
        ProductListingListFilter filter,
        CancellationToken cancellationToken);
}
