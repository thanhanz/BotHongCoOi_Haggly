using System.Text.RegularExpressions;
using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Discovery;

public sealed partial class CanonicalIngredient : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public ICollection<CommonDishIngredient> Dishes { get; } = new List<CommonDishIngredient>();
    public ICollection<ProductIngredientMapping> ProductMappings { get; } = new List<ProductIngredientMapping>();

    public static CanonicalIngredient Create(
        Guid id,
        string code,
        string name,
        string? category = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A valid ID is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        code = code.Trim();
        name = name.Trim();

        if (!CodePattern().IsMatch(code))
        {
            throw new ArgumentException("Code must match CI_[A-Z0-9_]+.", nameof(code));
        }

        return new CanonicalIngredient
        {
            Id = id,
            Code = code,
            Name = name,
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

    [GeneratedRegex("^CI_[A-Z0-9_]+$")]
    private static partial Regex CodePattern();
}
