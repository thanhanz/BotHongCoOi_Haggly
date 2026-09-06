namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryUnitOfWork
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
