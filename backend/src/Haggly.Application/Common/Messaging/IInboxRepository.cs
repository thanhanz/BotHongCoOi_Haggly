namespace Haggly.Application.Common.Messaging;

public interface IInboxRepository
{
    Task<bool> TryAddAsync(
        string consumerName,
        Guid eventId,
        string eventType,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken);
}
