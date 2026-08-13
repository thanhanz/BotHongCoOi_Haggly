using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface ICategoryQuery
{
    Task<IReadOnlyCollection<Category>> GetAllActiveAsync(CancellationToken cancellationToken);

    Task<Category?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken);
}
