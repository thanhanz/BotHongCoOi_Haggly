using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface IProductStallQuery
{
    Task<PagedResult<ProductStall>> GetProductsStallAsync(ProductStallListFilter filter, CancellationToken cancellationToken);
    Task<ProductStall?> GetActiveByIdAsync(Guid stallId, Guid id, CancellationToken cancellationToken);
}
