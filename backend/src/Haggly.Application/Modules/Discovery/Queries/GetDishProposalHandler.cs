using Haggly.Application.Abstractions.Discovery;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Domain.Modules.Discovery;
using MediatR;

namespace Haggly.Application.Modules.Discovery.Queries;

public sealed class GetDishProposalHandler(IDiscoveryQuery query)
    : IRequestHandler<GetDishProposalQuery, DishProposalResult>
{
    public async Task<DishProposalResult> Handle(
        GetDishProposalQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DishId == Guid.Empty)
        {
            throw new DiscoveryValidationException("A valid dish ID is required.");
        }

        var rows = await query.GetProposalRowsAsync(request.DishId, cancellationToken);
        if (rows.Count == 0
            && !await query.DishExistsAsync(request.DishId, cancellationToken))
        {
            throw new DiscoveryNotFoundException("The dish was not found.");
        }

        var ingredients = rows
            .GroupBy(row => new
            {
                row.CanonicalIngredientId,
                row.Code,
                row.IngredientName
            })
            .Select(group => CreateProposalIngredient(group))
            .OrderBy(ingredient => ingredient.Name, StringComparer.Ordinal)
            .ToList();

        return new DishProposalResult(
            request.DishId,
            rows.FirstOrDefault()?.DishName ?? string.Empty,
            ingredients);
    }

    private static ProposalIngredient CreateProposalIngredient(
        IEnumerable<ProposalSourceRow> sourceRows)
    {
        var rows = sourceRows.ToList();
        var first = rows[0];
        var hasProduct = rows.Any(row => row.ProductId is not null && row.ProductActive);
        var hasActiveListing = rows.Any(row =>
            row.ProductId is not null
            && row.ProductActive
            && row.ListingActive
            && row.StallActive
            && row.MarketActive);

        var eligible = rows
            .Where(IsEligible)
            .OrderByDescending(row => NameRelevance(row.ProductName!, row.IngredientName))
            .ThenBy(row => row.MinimumOrderQuantity * row.CurrentUnitPrice)
            .ThenBy(row => row.ProductId)
            .ThenBy(row => row.InventoryItemId)
            .Select(ToListing)
            .ToList();

        var status = eligible.Count > 0
            ? "AVAILABLE"
            : !hasProduct
                ? "NO_PRODUCT"
                : !hasActiveListing
                    ? "NO_ACTIVE_LISTING"
                    : "OUT_OF_STOCK";

        return new ProposalIngredient(
            first.CanonicalIngredientId,
            first.Code,
            first.IngredientName,
            status,
            eligible.FirstOrDefault(),
            eligible.Skip(1).ToList());
    }

    private static bool IsEligible(ProposalSourceRow row)
        => row.ProductId is not null
            && row.ProductActive
            && row.ListingActive
            && row.StallActive
            && row.MarketActive
            && row.InventoryItemId is not null
            && row.CurrentQuantity - row.ReservedQuantity >= row.MinimumOrderQuantity;

    private static int NameRelevance(string productName, string ingredientName)
    {
        var normalizedProductName = VietnameseNameNormalizer.Normalize(productName);
        var normalizedIngredientName = VietnameseNameNormalizer.Normalize(ingredientName);

        if (normalizedProductName == normalizedIngredientName)
        {
            return 2;
        }

        return normalizedProductName.Contains(normalizedIngredientName, StringComparison.Ordinal)
            ? 1
            : 0;
    }

    private static ProposalListing ToListing(ProposalSourceRow row)
        => new(
            row.ProductId!.Value,
            row.ProductName!,
            row.DisplayName ?? row.ProductName!,
            row.InventoryItemId!.Value,
            row.StallId!.Value,
            row.StallName!,
            row.SellingUnit!,
            row.MinimumOrderQuantity!.Value,
            row.CurrentUnitPrice!.Value,
            row.CurrentQuantity!.Value - row.ReservedQuantity!.Value);
}
