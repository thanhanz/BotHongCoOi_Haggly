using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Common.Messaging;

public interface IDomainEventPublisher
{
    Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}
