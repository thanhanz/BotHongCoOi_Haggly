namespace Haggly.Application.Abstractions.Sales;

public interface ICartCheckoutUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}
