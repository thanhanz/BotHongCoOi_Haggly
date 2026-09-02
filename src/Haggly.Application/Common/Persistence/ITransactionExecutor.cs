namespace Haggly.Application.Common.Persistence;

public interface ITransactionExecutor
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
