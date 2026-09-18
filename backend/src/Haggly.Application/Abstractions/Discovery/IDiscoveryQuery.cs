using Haggly.Application.Modules.Discovery.Dtos;

namespace Haggly.Application.Abstractions.Discovery;

public interface IDiscoveryQuery
{
    Task<IReadOnlyList<CommonDishResult>> FindDishesAsync(
        string normalizedQuery,
        CancellationToken cancellationToken);

    Task<bool> DishExistsAsync(
        Guid dishId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProposalSourceRow>> GetProposalRowsAsync(
        Guid dishId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CanonicalIngredientResult>> FindCanonicalIngredientsAsync(
        string normalizedQuery,
        CancellationToken cancellationToken);
}
