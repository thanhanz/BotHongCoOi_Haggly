using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Common.Messaging;

public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent;
}
