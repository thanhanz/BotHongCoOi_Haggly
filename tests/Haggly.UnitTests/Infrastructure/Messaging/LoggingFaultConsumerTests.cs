using System.Reflection;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging;
using Haggly.Infrastructure.Messaging.Faults;
using MassTransit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Messaging;

public sealed class LoggingFaultConsumerTests
{
    [Fact]
    public async Task Consume_PaymentSucceededFault_LogsComponentEventAndExceptionDetails()
    {
        var logger = new RecordingLogger<LoggingFaultConsumer<PaymentSucceededEvent>>();
        var consumer = new LoggingFaultConsumer<PaymentSucceededEvent>(logger);
        var occurredAt = new DateTimeOffset(2026, 8, 25, 1, 2, 3, TimeSpan.Zero);
        var message = CreateMessage(occurredAt);
        var faultId = Guid.NewGuid();
        var faultedMessageId = Guid.NewGuid();
        var fault = new TestFault<PaymentSucceededEvent>(
            faultId,
            faultedMessageId,
            occurredAt.AddSeconds(1).UtcDateTime,
            [
                new TestExceptionInfo(
                    "System.InvalidOperationException",
                    null,
                    "at InventoryPaymentSucceededHandler.HandleAsync()",
                    "The paid order has no active inventory items.",
                    "Haggly.Application"),
                new TestExceptionInfo(
                    "Npgsql.PostgresException",
                    null,
                    "at NpgsqlCommand.ExecuteReaderAsync()",
                    "The database command failed.",
                    "Npgsql")
            ],
            new TestHostInfo("haggly-worker"),
            [typeof(PaymentSucceededEvent).FullName!],
            message);
        var context = CreateContext(
            fault,
            message.CorrelationId,
            new Uri(
                $"rabbitmq://localhost/{PaymentMessagingNames.InventoryPaymentSucceededQueue}"));

        await consumer.Consume(context);

        var log = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Null(log.Exception);
        Assert.Equal("Inventory", log.Properties["Component"]);
        Assert.Equal(typeof(PaymentSucceededEvent).FullName, log.Properties["EventType"]);
        Assert.Equal(faultId, log.Properties["FaultId"]);
        Assert.Equal(faultedMessageId, log.Properties["FaultedMessageId"]);
        Assert.Equal(message.CorrelationId, log.Properties["CorrelationId"]);
        Assert.Equal(message.EventId, log.Properties["EventId"]);
        Assert.Equal("haggly-worker", log.Properties["HostMachine"]);

        var exceptions = Assert.IsAssignableFrom<IReadOnlyList<EventFaultException>>(
            log.Properties["Exceptions"]);
        Assert.Collection(
            exceptions,
            exception =>
            {
                Assert.Equal("System.InvalidOperationException", exception.ExceptionType);
                Assert.Equal("The paid order has no active inventory items.", exception.Message);
                Assert.Equal("at InventoryPaymentSucceededHandler.HandleAsync()", exception.StackTrace);
            },
            exception =>
            {
                Assert.Equal("Npgsql.PostgresException", exception.ExceptionType);
                Assert.Equal("The database command failed.", exception.Message);
                Assert.Equal("at NpgsqlCommand.ExecuteReaderAsync()", exception.StackTrace);
            });

        var exceptionDetails = Assert.IsType<string>(log.Properties["ExceptionDetails"]);
        Assert.Contains("System.InvalidOperationException", exceptionDetails);
        Assert.Contains("The paid order has no active inventory items.", exceptionDetails);
        Assert.Contains("Npgsql.PostgresException", exceptionDetails);
        Assert.Contains("The database command failed.", exceptionDetails);
    }

    [Fact]
    public async Task Consume_WhenLoggingSucceeds_CompletesWithoutThrowing()
    {
        var logger = new RecordingLogger<LoggingFaultConsumer<PaymentSucceededEvent>>();
        var consumer = new LoggingFaultConsumer<PaymentSucceededEvent>(logger);
        var occurredAt = new DateTimeOffset(2026, 8, 25, 1, 2, 3, TimeSpan.Zero);
        var message = CreateMessage(occurredAt);
        var fault = new TestFault<PaymentSucceededEvent>(
            Guid.NewGuid(),
            Guid.NewGuid(),
            occurredAt.AddSeconds(1).UtcDateTime,
            [],
            new TestHostInfo("haggly-worker"),
            [typeof(PaymentSucceededEvent).FullName!],
            message);
        var context = CreateContext(
            fault,
            message.CorrelationId,
            new Uri($"rabbitmq://localhost/{PaymentMessagingNames.OrderPaymentSucceededQueue}"));

        await consumer.Consume(context);

        Assert.Single(logger.Entries);
    }

    private static PaymentSucceededEvent CreateMessage(DateTimeOffset occurredAt)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            occurredAt,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            300_000m,
            "VND",
            "provider-transaction-1",
            [Guid.NewGuid()]);

    private static ConsumeContext<Fault<PaymentSucceededEvent>> CreateContext(
        Fault<PaymentSucceededEvent> fault,
        Guid correlationId,
        Uri sourceAddress)
    {
        var context = DispatchProxy.Create<ConsumeContext<Fault<PaymentSucceededEvent>>, ConsumeContextProxy>();
        var proxy = (ConsumeContextProxy)(object)context;
        proxy.Message = fault;
        proxy.CorrelationId = correlationId;
        proxy.SourceAddress = sourceAddress;
        return context;
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        public object? Message { get; set; }
        public Guid? CorrelationId { get; set; }
        public Uri? SourceAddress { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => targetMethod?.Name switch
            {
                "get_Message" => Message,
                "get_CorrelationId" => CorrelationId,
                "get_SourceAddress" => SourceAddress,
                _ => throw new NotSupportedException(
                    $"The test consume context does not implement '{targetMethod?.Name}'.")
            };
    }

    private sealed record TestFault<T>(
        Guid FaultId,
        Guid? FaultedMessageId,
        DateTime Timestamp,
        ExceptionInfo[] Exceptions,
        HostInfo Host,
        string[] FaultMessageTypes,
        T Message) : Fault<T>
        where T : class;

    private sealed record TestExceptionInfo(
        string ExceptionType,
        ExceptionInfo? InnerException,
        string StackTrace,
        string Message,
        string Source) : ExceptionInfo
    {
        public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();
    }

    private sealed record TestHostInfo(string MachineName) : HostInfo
    {
        public string ProcessName => "Haggly.Api";
        public int ProcessId => 123;
        public string Assembly => "Haggly.Api";
        public string AssemblyVersion => "1.0.0";
        public string FrameworkVersion => ".NET 10";
        public string MassTransitVersion => "8.5.10";
        public string OperatingSystemVersion => "Test OS";
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state as IEnumerable<KeyValuePair<string, object?>>
                ?? [];
            Entries.Add(new LogEntry(
                logLevel,
                exception,
                properties.ToDictionary(item => item.Key, item => item.Value)));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties);
}
