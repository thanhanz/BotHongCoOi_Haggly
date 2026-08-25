using Haggly.Domain.Common.Events.V1;

namespace Haggly.Infrastructure.Messaging.Faults;

public static class EventFaultLogMapper
{
    public static EventFaultLogEntry<TEvent> Map<TEvent>(
        EventFaultMetadata<TEvent> fault)
        where TEvent : class, IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(fault);

        return new EventFaultLogEntry<TEvent>(
            ResolveComponent(fault.SourceAddress),
            fault);
    }

    private static string ResolveComponent(string? sourceAddress)
    {
        if (!Uri.TryCreate(sourceAddress, UriKind.Absolute, out var sourceUri))
            return "Unknown";

        var queueName = sourceUri.Segments.LastOrDefault()?.Trim('/');
        if (string.IsNullOrWhiteSpace(queueName))
            return "Unknown";

        var queueParts = queueName.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (queueParts.Length < 2)
            return "Unknown";

        var component = queueParts[0];
        return char.ToUpperInvariant(component[0]) + component[1..].ToLowerInvariant();
    }
}
