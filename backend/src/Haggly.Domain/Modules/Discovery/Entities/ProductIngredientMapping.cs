using Haggly.Domain.Modules.Catalog;

namespace Haggly.Domain.Modules.Discovery;

/// <summary>
/// 
/// Mapping Catelog.Product to Discovery.CanonicalIngredient
/// 
/// A Product can have zero or one mapping to a Canonical Ingredient
/// - This entity supports approved and rejected desicions without changing Product state.
/// - Mapping changes can be autdited 
/// - Unmapped Products remain valid Catalog.Product
/// 
/// </summary>

public sealed class ProductIngredientMapping
{
    private ProductIngredientMapping()
    {
    }

    public ProductIngredientMapping(
        Guid productId,
        Guid canonicalIngredientId,
        ProductIngredientMappingStatus status,
        ProductIngredientMappingMethod method,
        DateTimeOffset at)
    {
        if (productId == Guid.Empty || canonicalIngredientId == Guid.Empty)
        {
            throw new ArgumentException("Valid product and ingredient IDs are required.");
        }

        ProductId = productId;
        CanonicalIngredientId = canonicalIngredientId;
        Status = status;
        MappingMethod = method;
        CreatedAt = at;
    }

    public Guid ProductId { get; private set; }
    public Guid CanonicalIngredientId { get; private set; }
    public ProductIngredientMappingStatus Status { get; private set; }
    public ProductIngredientMappingMethod MappingMethod { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Product? Product { get; private set; }
    public CanonicalIngredient? CanonicalIngredient { get; private set; }

    public void Replace(
        Guid canonicalIngredientId,
        ProductIngredientMappingStatus status,
        ProductIngredientMappingMethod method,
        DateTimeOffset at)
    {
        if (canonicalIngredientId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid ingredient ID is required.",
                nameof(canonicalIngredientId));
        }

        CanonicalIngredientId = canonicalIngredientId;
        Status = status;
        MappingMethod = method;
        UpdatedAt = at;
    }
}
