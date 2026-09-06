using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Queries.Products;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface IProductQuery
{
    Task<PagedResult<Product>> GetPageAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken);

    Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken);
}
