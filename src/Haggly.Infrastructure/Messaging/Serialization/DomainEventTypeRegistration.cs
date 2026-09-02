using Haggly.Domain.Common.Events.V1;

namespace Haggly.Infrastructure.Messaging.Serialization;

public sealed record DomainEventTypeRegistration(string EventType, Type ClrType)
{
    public static DomainEventTypeRegistration For<TEvent>(string eventType)
        where TEvent : class, IDomainEvent
        => new(eventType, typeof(TEvent));
}
