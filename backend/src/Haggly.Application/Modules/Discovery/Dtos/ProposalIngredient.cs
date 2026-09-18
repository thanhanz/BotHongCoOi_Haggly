namespace Haggly.Application.Modules.Discovery.Dtos;

public sealed record ProposalIngredient(
    Guid CanonicalIngredientId,
    string Code,
    string Name,
    string Status,
    ProposalListing? SelectedListing,
    IReadOnlyList<ProposalListing> Alternatives);
