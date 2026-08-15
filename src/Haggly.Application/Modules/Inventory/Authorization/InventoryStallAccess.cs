using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Modules.Inventory.Authorization;

internal static class InventoryStallAccess
{
    public static async Task<Stall> RequireOwnedActiveStallAsync(
        IInventoryReferenceQuery references,
        Guid stallId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var stall = await references.FindActiveStallAsync(stallId, cancellationToken)
            ?? throw new InventoryNotFoundException("The stall was not found.");

        if (stall.Status != StallStatus.ACTIVE || stall.DeletedAt is not null)
        {
            throw new InventoryNotFoundException("The stall was not found.");
        }

        if (stall.VendorId != actorUserId)
        {
            throw new InventoryForbiddenException("Only the stall owner can access inventory.");
        }

        return stall;
    }

    public static async Task<ProductStall> RequireActiveProductStallAsync(
        IInventoryReferenceQuery references,
        Guid stallId,
        Guid productStallId,
        CancellationToken cancellationToken)
    {
        var productStall = await references.FindActiveProductStallAsync(
                stallId,
                productStallId,
                cancellationToken)
            ?? throw new InventoryNotFoundException("The product was not found in this stall.");

        if (productStall.StallId != stallId
            || !productStall.IsActive
            || productStall.DeletedAt is not null
            || productStall.Product is null
            || productStall.Product.DeletedAt is not null
            || productStall.Product.Status != CatalogStatus.ACTIVE)
        {
            throw new InventoryNotFoundException("The product was not found in this stall.");
        }

        return productStall;
    }
}
