using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventoryPaymentRepository(HagglyDbContext dbContext)
    : IInventoryPaymentRepository
{
    public async Task ReserveAsync(
        Guid orderId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var orderItems = await FindActiveOrderItemsAsync(orderId, cancellationToken);
        if (orderItems.Count == 0)
            throw new InventoryConflictException("The order has no active inventory items to reserve.");

        var groups = GroupByInventoryItem(orderItems).ToArray();
        if (groups.Any(group => group.Quantity > group.InventoryItem.AvailableQuantity))
            throw new InventoryConflictException("The order quantity exceeds available inventory.");

        foreach (var group in groups)
        {
            group.InventoryItem.Reserve(group.Quantity, occurredAt);
        }

        await SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(
        Guid orderId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var orderItems = await FindActiveOrderItemsAsync(orderId, cancellationToken);
        if (orderItems.Count == 0)
            throw new InventoryConflictException("The order has no active inventory items to release.");

        var groups = GroupByInventoryItem(orderItems).ToArray();
        if (groups.Any(group => group.Quantity > group.InventoryItem.ReservedQuantity))
            throw new InventoryConflictException("The order quantity exceeds reserved inventory.");

        foreach (var group in groups)
        {
            group.InventoryItem.ReleaseReserved(group.Quantity, occurredAt);
        }

        await SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasProcessedAsync(
        Guid paymentTransactionId,
        InventoryTransactionType transactionType,
        CancellationToken cancellationToken)
        => dbContext.InventoryLedgers.AnyAsync(ledger => 
            ledger.TransactionType == transactionType && 
            ledger.ReferenceId == paymentTransactionId,
            cancellationToken);

    public async Task<IReadOnlyList<OrderItem>> FindActiveOrderItemsAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => await dbContext.OrderItems
            .Include(item => item.InventoryItem)!
                .ThenInclude(item => item!.InventoryLedgers)
            .Where(item => item.StallFulfillment!.OrderId == orderId
                           && item.Status == OrderItemStatus.ACTIVE)
            .ToArrayAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConflictException(
                "Inventory changed while processing the payment.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InventoryConflictException(
                "The successful payment was already applied to inventory.");
        }
    }

    private static IEnumerable<(InventoryItem InventoryItem, decimal Quantity)> GroupByInventoryItem(
        IReadOnlyList<OrderItem> orderItems)
        => orderItems
            .GroupBy(item => item.InventoryItemId)
            .Select(group => (
                group.First().InventoryItem
                    ?? throw new InventoryConflictException(
                        $"Inventory item '{group.Key}' was not found."),
                group.Sum(item => item.FinalQuantity)));
}
