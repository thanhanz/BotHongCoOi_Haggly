using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface IProductQuery
{
    Task<IReadOnlyCollection<Product>> GetAllActiveAsync(
        Guid? categoryId,
        CancellationToken cancellationToken);

    Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken);
}
