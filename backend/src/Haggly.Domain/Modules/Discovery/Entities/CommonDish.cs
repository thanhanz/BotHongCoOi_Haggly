using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Discovery;

public sealed class CommonDish : Entity
{
    public string ExternalId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public ICollection<CommonDishIngredient> Ingredients { get; } = new List<CommonDishIngredient>();

    public static CommonDish Create(
        Guid id,
        string externalId,
        string name,
        string? category = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A valid ID is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CommonDish
        {
            Id = id,
            ExternalId = externalId.Trim(),
            Name = name.Trim(),
            NormalizedName = VietnameseNameNormalizer.Normalize(name),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            IsActive = true
        };
    }

    public void Update(
        string name,
        string normalizedName,
        string? category,
        DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        Name = name.Trim();
        NormalizedName = normalizedName.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        UpdatedAt = at;
    }
}
