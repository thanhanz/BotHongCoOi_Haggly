using Haggly.Application.Common.Messaging;
using Haggly.Domain.Common.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging;

public sealed class MassTransitDomainEventPublisher(IPublishEndpoint publishEndpoint)
    : IDomainEventPublisher
{
    public Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent
        => publishEndpoint.Publish(integrationEvent, cancellationToken);
}
