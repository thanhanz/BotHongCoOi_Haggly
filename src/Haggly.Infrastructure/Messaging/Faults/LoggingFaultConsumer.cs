using Haggly.Domain.Common.Events.V1;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Haggly.Infrastructure.Messaging.Faults;

public sealed class LoggingFaultConsumer<TEvent>(
    ILogger<LoggingFaultConsumer<TEvent>> logger): IConsumer<Fault<TEvent>> 
    where TEvent : class, IDomainEvent
{
    public Task Consume(ConsumeContext<Fault<TEvent>> context)
    {
        var fault = context.Message;

        var metadata = new EventFaultMetadata<TEvent>(
            fault.FaultId,
            fault.FaultedMessageId,
            context.CorrelationId,
            new DateTimeOffset(fault.Timestamp.ToUniversalTime()),
            context.SourceAddress?.ToString(),
            fault.Host.MachineName ?? "Unknown",
            fault.Message,
            fault.Exceptions
                .Select(exception => new EventFaultException(
                    exception.ExceptionType,
                    exception.Message,
                    exception.StackTrace))
                .ToArray());
        var entry = EventFaultLogMapper.Map(metadata);
        var exceptionDetails = string.Join(
            Environment.NewLine,
            entry.Fault.Exceptions.Select(exception =>
                $"{exception.ExceptionType}: {exception.Message}{Environment.NewLine}{exception.StackTrace}"));

        logger.LogError(
            "Event processing failed. Component={Component}, EventType={EventType}, "
            + "FaultId={FaultId}, FaultedMessageId={FaultedMessageId}, "
            + "CorrelationId={CorrelationId}, EventId={EventId}, "
            + "SourceAddress={SourceAddress}, HostMachine={HostMachine}, "
            + "Exceptions={Exceptions}, ExceptionDetails={ExceptionDetails}",
            entry.Component,
            typeof(TEvent).FullName,
            entry.Fault.FaultId,
            entry.Fault.FaultedMessageId,
            entry.Fault.CorrelationId,
            entry.Fault.Message.EventId,
            entry.Fault.SourceAddress,
            entry.Fault.HostMachine,
            entry.Fault.Exceptions,
            exceptionDetails);

        return Task.CompletedTask;
    }
}
