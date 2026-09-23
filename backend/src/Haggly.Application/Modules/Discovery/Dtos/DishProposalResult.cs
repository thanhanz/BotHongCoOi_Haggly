namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record DishProposalResult(
    Guid DishId,
    string DishName,
    IReadOnlyList<ProposalIngredient> Ingredients);
