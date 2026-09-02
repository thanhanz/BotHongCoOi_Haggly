using Haggly.Domain.Common.Events.V1;

namespace Haggly.Infrastructure.Messaging.Serialization;

public sealed class DomainEventTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> typesByName;
    private readonly IReadOnlyDictionary<Type, string> namesByType;

    public DomainEventTypeRegistry(IEnumerable<DomainEventTypeRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        var byName = new Dictionary<string, Type>(StringComparer.Ordinal);
        var byType = new Dictionary<Type, string>();

        foreach (var registration in registrations)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(registration.EventType);
            if (!typeof(IDomainEvent).IsAssignableFrom(registration.ClrType))
            {
                throw new ArgumentException(
                    $"Type '{registration.ClrType.FullName}' must implement {nameof(IDomainEvent)}.",
                    nameof(registrations));
            }

            if (!byName.TryAdd(registration.EventType, registration.ClrType))
            {
                throw new ArgumentException(
                    $"Domain event name '{registration.EventType}' is registered more than once.",
                    nameof(registrations));
            }

            if (!byType.TryAdd(registration.ClrType, registration.EventType))
            {
                throw new ArgumentException(
                    $"Domain event type '{registration.ClrType.FullName}' is registered more than once.",
                    nameof(registrations));
            }
        }

        typesByName = byName;
        namesByType = byType;
    }

    public string GetEventType(Type clrType)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        return namesByType.TryGetValue(clrType, out var eventType)
            ? eventType
            : throw new InvalidOperationException(
                $"Domain event type '{clrType.FullName}' has not been registered.");
    }

    public Type GetClrType(string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        return typesByName.TryGetValue(eventType, out var clrType)
            ? clrType
            : throw new InvalidOperationException(
                $"Domain event name '{eventType}' has not been registered.");
    }
}
