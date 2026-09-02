using Haggly.Application.Common.Messaging;
using Haggly.Domain.Common.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging;

public sealed class MassTransitDomainEventPublisher(IPublishEndpoint publishEndpoint)
    : IDomainEventPublisher
{
    public Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken);
    }
}
