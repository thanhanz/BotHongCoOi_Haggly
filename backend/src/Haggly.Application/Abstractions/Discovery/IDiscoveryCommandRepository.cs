using Haggly.Domain.Modules.Discovery;

namespace Haggly.Application.Abstractions.Discovery;

public interface IDiscoveryCommandRepository
{
    Task SetProductMappingAsync(
        Guid productId,
        Guid? canonicalIngredientId,
        ProductIngredientMappingStatus status,
        ProductIngredientMappingMethod method,
        DateTimeOffset at,
        CancellationToken cancellationToken);
}
