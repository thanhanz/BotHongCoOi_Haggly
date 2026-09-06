using Haggly.Application.Abstractions.Payments;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Payments;

public sealed class EfPaymentUnitOfWork(HagglyDbContext dbContext) : IPaymentUnitOfWork
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

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
