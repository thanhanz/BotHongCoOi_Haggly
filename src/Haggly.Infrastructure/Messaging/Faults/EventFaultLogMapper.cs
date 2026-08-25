using Haggly.Domain.Common.Events.V1;

namespace Haggly.Infrastructure.Messaging.Faults;

public static class EventFaultLogMapper
{
    public static EventFaultLogEntry<TEvent> Map<TEvent>(
        EventFaultMetadata<TEvent> fault)
        where TEvent : class, IDomainEvent
        => throw new NotImplementedException();
}
