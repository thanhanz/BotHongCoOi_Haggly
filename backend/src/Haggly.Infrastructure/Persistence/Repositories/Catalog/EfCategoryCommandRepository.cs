using Haggly.Application.Abstractions.Catalog;
using Haggly.Domain.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Catalog;

public sealed class EfCategoryCommandRepository(HagglyDbContext dbContext)
    : ICategoryCommandRepository
{
    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken)
        => dbContext.Categories.AnyAsync(
            category => category.Slug == slug
                && (excludingId == null || category.Id != excludingId),
            cancellationToken);

    public Task<Category?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        dbContext.Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
