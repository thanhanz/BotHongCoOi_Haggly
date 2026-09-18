using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Discovery;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Discovery;

public sealed class EfDiscoveryCommandRepository(HagglyDbContext dbContext)
    : IDiscoveryCommandRepository
{
    public async Task SetProductMappingAsync(
        Guid productId,
        Guid? canonicalIngredientId,
        ProductIngredientMappingStatus status,
        ProductIngredientMappingMethod method,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        var productExists = await dbContext.Products.AnyAsync(
            product => product.Id == productId && product.Status == CatalogStatus.ACTIVE,
            cancellationToken);

        if (!productExists)
        {
            throw new DiscoveryNotFoundException("The product was not found.");
        }

        var existing = await dbContext.ProductIngredientMappings.SingleOrDefaultAsync(
            mapping => mapping.ProductId == productId,
            cancellationToken);

        if (canonicalIngredientId is null)
        {
            if (existing is not null)
            {
                dbContext.ProductIngredientMappings.Remove(existing);
            }
        }
        else
        {
            var ingredientExists = await dbContext.CanonicalIngredients.AnyAsync(
                ingredient => ingredient.Id == canonicalIngredientId && ingredient.IsActive,
                cancellationToken);

            if (!ingredientExists)
            {
                throw new DiscoveryNotFoundException("The canonical ingredient was not found.");
            }

            if (existing is null)
            {
                dbContext.ProductIngredientMappings.Add(new ProductIngredientMapping(
                    productId,
                    canonicalIngredientId.Value,
                    status,
                    method,
                    at));
            }
            else
            {
                existing.Replace(canonicalIngredientId.Value, status, method, at);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
