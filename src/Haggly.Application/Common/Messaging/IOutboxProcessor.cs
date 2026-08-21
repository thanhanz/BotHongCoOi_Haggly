namespace Haggly.Application.Common.Messaging;

public interface IOutboxProcessor
{
    Task<int> ProcessPendingAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);
}
