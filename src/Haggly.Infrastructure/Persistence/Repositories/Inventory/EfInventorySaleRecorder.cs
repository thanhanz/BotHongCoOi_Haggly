using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Sales.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventorySaleRecorder(HagglyDbContext dbContext) : IInventorySaleRecorder
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
            .Include(item => item.Inventory)
            .Include(item => item.ProductStall)!.ThenInclude(productStall => productStall!.Product)
            .Where(item => item.Inventory!.StallId == stallId && itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var snapshots = new List<InventorySaleItemSnapshot>(lines.Count);
        foreach (var line in lines)
        {
            if (!items.TryGetValue(line.InventoryItemId, out var item))
                throw new PosSaleNotFoundException("The inventory item was not found.");
            if (item.ProductStall is null || !item.ProductStall.IsActive)
                throw new PosSaleConflictException("The stall product is not available for sale.");
            if (item.Version != line.ExpectedVersion)
                throw new PosSaleConflictException("The inventory item was changed by another request. Refresh and retry.");

            try
            {
                item.RecordSale(line.Quantity, saleId, actorId, occurredAt);
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
                item.Version));
        }

        return snapshots;
    }
}
