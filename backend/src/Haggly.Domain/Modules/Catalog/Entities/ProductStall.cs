using Haggly.Domain.Common;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Domain.Modules.Catalog;

public sealed class ProductStall : SoftDeletableEntity
{
    public Guid StallId { get; set; }
    public Guid ProductId { get; set; }
    public string? DisplayName { get; set; }
    public ProductUnit SellingUnit { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public decimal CurrentUnitPrice { get; private set; }
    public bool IsNegotiable { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; private set; }

    public Stall? Stall { get; set; }
    public Product? Product { get; set; }

    public static ProductStall Create(
        Guid stallId,
        Guid productId,
        string? displayName,
        ProductUnit sellingUnit,
        decimal minimumOrderQuantity,
        decimal currentUnitPrice,
        bool isNegotiable)
    {
        if (stallId == Guid.Empty || productId == Guid.Empty)
        {
            throw new ArgumentException("Valid stall and product IDs are required.");
        }

        if (!Enum.IsDefined(sellingUnit) || minimumOrderQuantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOrderQuantity), "Selling unit and minimum quantity are invalid.");
        }

        if (currentUnitPrice < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(currentUnitPrice), "Current unit price cannot be negative.");
        }

        return new ProductStall
        {
            StallId = stallId,
            ProductId = productId,
            DisplayName = displayName?.Trim(),
            SellingUnit = sellingUnit,
            MinimumOrderQuantity = minimumOrderQuantity,
            CurrentUnitPrice = currentUnitPrice,
            IsNegotiable = isNegotiable,
            IsActive = true
        };
    }

    public void UpdateConfiguration(
        string? displayName,
        ProductUnit? sellingUnit,
        decimal? minimumOrderQuantity,
        decimal? currentUnitPrice,
        bool? isNegotiable,
        bool? isActive)
    {
        if (sellingUnit is not null && !Enum.IsDefined(sellingUnit.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(sellingUnit), "Selling unit is invalid.");
        }

        if (minimumOrderQuantity is <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOrderQuantity), "Minimum quantity must be greater than zero.");
        }

        if (currentUnitPrice is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(currentUnitPrice), "Current unit price cannot be negative.");
        }

        var changed = false;
        if (displayName is not null && DisplayName != displayName.Trim())
        {
            DisplayName = displayName.Trim();
            changed = true;
        }

        if (sellingUnit is not null && SellingUnit != sellingUnit.Value)
        {
            SellingUnit = sellingUnit.Value;
            changed = true;
        }

        if (minimumOrderQuantity is not null && MinimumOrderQuantity != minimumOrderQuantity.Value)
        {
            MinimumOrderQuantity = minimumOrderQuantity.Value;
            changed = true;
        }

        if (currentUnitPrice is not null && CurrentUnitPrice != currentUnitPrice.Value)
        {
            CurrentUnitPrice = currentUnitPrice.Value;
            changed = true;
        }

        if (isNegotiable is not null && IsNegotiable != isNegotiable.Value)
        {
            IsNegotiable = isNegotiable.Value;
            changed = true;
        }

        if (isActive is not null && IsActive != isActive.Value)
        {
            IsActive = isActive.Value;
            changed = true;
        }

        if (changed)
        {
            Version++;
        }
    }
}
