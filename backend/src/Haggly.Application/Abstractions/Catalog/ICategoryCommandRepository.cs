using Haggly.Domain.Modules.Catalog;

namespace Haggly.Application.Abstractions.Catalog;

public interface ICategoryCommandRepository
{
    Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
