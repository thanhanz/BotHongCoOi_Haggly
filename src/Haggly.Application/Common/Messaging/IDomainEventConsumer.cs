using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Common.Messaging;

public interface IDomainEventConsumer<in TEvent>
    where TEvent : class, IDomainEvent
{
    Task ConsumeAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken);
}
