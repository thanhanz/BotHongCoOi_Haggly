using Haggly.Domain.Common.Events.V1;

namespace Haggly.Infrastructure.Messaging.Faults;

public sealed record EventFaultLogEntry<TEvent>(
    string Component,
    EventFaultMetadata<TEvent> Fault) where TEvent : class, IDomainEvent;


public sealed record EventFaultMetadata<TEvent>(
    Guid? FaultId,
    Guid? FaultedMessageId,
    Guid? CorrelationId,
    DateTimeOffset FaultedAt,
    string? SourceAddress,
    string HostMachine,
    TEvent Message,
    IReadOnlyList<EventFaultException> Exceptions) where TEvent : class, IDomainEvent;

public sealed record EventFaultException(
    string ExceptionType,
    string Message,
    string? StackTrace);


/** Summary>
EventFaultLogEntry<TEvent>
│
├── Component (Payment.Consumers)
│
└── EventFaultMetadata<TEvent>
    │
    ├── FaultId
    ├── FaultedMessageId
    ├── CorrelationId
    ├── FaultedAt
    ├── SourceAddress
    ├── HostMachine
    ├── Message
    │
    └── Exceptions[]
        ├── EventFaultException
        ├── EventFaultException
        └── ...
**/
