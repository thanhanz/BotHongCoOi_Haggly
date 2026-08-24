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
                "Inventory changed while applying the successful payment.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InventoryConflictException(
                "The successful payment was already applied to inventory.");
        }
    }
}
