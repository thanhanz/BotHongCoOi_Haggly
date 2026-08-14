using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Abstractions.Catalog;

public interface IProductStallCommandRepository
{
    Task<Stall?> FindActiveStallAsync(Guid id, CancellationToken cancellationToken);
    Task<Product?> FindActiveProductAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid stallId, Guid productId, CancellationToken cancellationToken);
    Task<ProductStall?> FindActiveAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ProductStall productStall, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
