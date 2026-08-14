using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface IProductCommandRepository
{
    Task<bool> NameExistsAsync(
        Guid categoryId,
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<Category?> FindActiveCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
