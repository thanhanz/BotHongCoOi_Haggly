using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Sales.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventorySaleRepository(HagglyDbContext dbContext) : IInventorySaleRepository
{
    public async Task<IReadOnlyList<InventorySaleItemSnapshot>> RecordPosSaleAsync(
        Guid stallId,
        Guid saleId,
        Guid actorId,
        IReadOnlyCollection<InventorySaleLine> lines,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var itemIds = lines.Select(line => line.InventoryItemId).ToArray();
        var items = await dbContext.InventoryItems
            .Include(item => item.Inventory)!.ThenInclude(inventory => inventory!.Stall)
            .Include(item => item.ProductStall)!.ThenInclude(productStall => productStall!.Product)
            .Where(item => item.Inventory!.StallId == stallId && itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        // Price and selling unit are sale inputs. Lock each ProductStall row and
        // reload it so a concurrent Catalog update cannot commit between the
        // expected-version check and the immutable sale snapshot.
        foreach (var productStall in items.Values
                     .Select(item => item.ProductStall)
                     .Where(value => value is not null)
                     .DistinctBy(value => value!.Id))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM catalog.product_stalls WHERE \"Id\" = {productStall!.Id} FOR UPDATE",
                cancellationToken);
            await dbContext.Entry(productStall).ReloadAsync(cancellationToken);
        }

        var snapshots = new List<InventorySaleItemSnapshot>(lines.Count);
        var inventory = items.Values.Select(item => item.Inventory).FirstOrDefault();
        if (inventory?.Stall is null
            || inventory.Stall.Status != Haggly.Domain.Modules.Markets.StallStatus.ACTIVE
            || inventory.Stall.DeletedAt is not null)
        {
            throw new PosSaleNotFoundException("The stall was not found.");
        }

        if (inventory.Stall.VendorId != actorId)
        {
            throw new PosSaleForbiddenException("Only the stall owner can record a POS sale.");
        }

        foreach (var line in lines)
        {
            if (!items.TryGetValue(line.InventoryItemId, out var item))
                throw new PosSaleNotFoundException("The inventory item was not found.");
            if (item.ProductStall is null || !item.ProductStall.IsActive)
                throw new PosSaleConflictException("The stall product is not available for sale.");
            if (item.Version != line.ExpectedInventoryVersion)
                throw new PosSaleConflictException("The inventory item was changed by another request. Refresh and retry.");
            if (item.ProductStall.Version != line.ExpectedProductStallVersion)
                throw new PosSaleConflictException("The stall product price or configuration changed. Refresh and retry.");

            try
            {
                item.RecordSaleDirectly(line.Quantity, saleId, actorId, occurredAt);
            }
            catch (InvalidOperationException exception)
            {
                throw new PosSaleConflictException(exception.Message);
            }

            var productName = item.ProductStall.DisplayName ?? item.ProductStall.Product!.Name;
            snapshots.Add(new InventorySaleItemSnapshot(
                item.Id,
                productName,
                item.ProductStall.SellingUnit,
                item.ProductStall.CurrentUnitPrice,
                line.Quantity,
                item.Version,
                item.ProductStall.Version));
        }

        return snapshots;
    }
}
