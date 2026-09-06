using Haggly.Application.Abstractions.Sales;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Transactions.Sales;

public sealed class EfSalesTransactionExecutor(HagglyDbContext dbContext)
    : ISalesTransactionExecutor
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("A database transaction is already active.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
