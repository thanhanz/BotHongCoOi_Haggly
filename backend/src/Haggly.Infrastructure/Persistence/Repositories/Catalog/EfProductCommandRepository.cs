using Haggly.Application.Abstractions.Catalog;
using Haggly.Domain.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Catalog;

public sealed class EfProductCommandRepository(HagglyDbContext dbContext)
    : IProductCommandRepository
{
    public Task<bool> NameExistsAsync(
        Guid categoryId,
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken)
        => dbContext.Products.AnyAsync(
            product => product.CategoryId == categoryId
                && product.Name == name
                && (excludingId == null || product.Id != excludingId),
            cancellationToken);

    public Task<Category?> FindActiveCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken)
        => dbContext.Categories.SingleOrDefaultAsync(
            category => category.Id == categoryId && category.Status == CatalogStatus.ACTIVE,
            cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        dbContext.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
