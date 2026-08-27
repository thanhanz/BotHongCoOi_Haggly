using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Messaging;

public sealed class PaymentMessagingTopologyTests
{
    [Fact]
    public async Task StartAsync_WhenPaymentTopologyConfigured_DeclaresDurableQueueAndBinding()
    {
        var services = new ServiceCollection();
        services.AddMessaging(CreateConfiguration());
        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(CancellationToken.None);
        try
        {
            using var client = CreateManagementClient();
            using var queue = await GetJsonAsync(
                client,
                $"queues/%2F/{PaymentMessagingNames.PaymentRequestedQueue}");
            Assert.True(queue.RootElement.GetProperty("durable").GetBoolean());
            Assert.False(queue.RootElement.GetProperty("auto_delete").GetBoolean());

            using var exchange = await GetJsonAsync(
                client,
                $"exchanges/%2F/{PaymentMessagingNames.PaymentRequestedExchange}");
            Assert.Equal("fanout", exchange.RootElement.GetProperty("type").GetString());
            Assert.True(exchange.RootElement.GetProperty("durable").GetBoolean());

            using var bindings = await GetJsonAsync(client, "bindings/%2F");
            Assert.Contains(bindings.RootElement.EnumerateArray(), binding =>
                binding.GetProperty("source").GetString()
                    == PaymentMessagingNames.PaymentRequestedExchange
                && binding.GetProperty("destination").GetString()
                    == PaymentMessagingNames.PaymentRequestedQueue
                && binding.GetProperty("destination_type").GetString() == "exchange");

            using var financeQueueDocument = await GetJsonAsync(
                client,
                $"queues/%2F/{PaymentMessagingNames.FinancePaymentSucceededQueue}");
            Assert.True(financeQueueDocument.RootElement.GetProperty("durable").GetBoolean());
            Assert.False(financeQueueDocument.RootElement.GetProperty("auto_delete").GetBoolean());

            Assert.Contains(bindings.RootElement.EnumerateArray(), binding =>
                binding.GetProperty("source").GetString()
                    == PaymentMessagingNames.PaymentSucceededExchange
                && binding.GetProperty("destination").GetString()
                    == PaymentMessagingNames.FinancePaymentSucceededQueue
                && binding.GetProperty("destination_type").GetString() == "exchange");

            using var inventoryQueueDocument = await GetJsonAsync(
                client,
                $"queues/%2F/{PaymentMessagingNames.InventoryPaymentSucceededQueue}");
            Assert.True(inventoryQueueDocument.RootElement.GetProperty("durable").GetBoolean());
            Assert.False(inventoryQueueDocument.RootElement.GetProperty("auto_delete").GetBoolean());

            Assert.Contains(bindings.RootElement.EnumerateArray(), binding =>
                binding.GetProperty("source").GetString()
                    == PaymentMessagingNames.PaymentSucceededExchange
                && binding.GetProperty("destination").GetString()
                    == PaymentMessagingNames.InventoryPaymentSucceededQueue
                && binding.GetProperty("destination_type").GetString() == "exchange");

            using var inventoryFailedQueueDocument = await GetJsonAsync(
                client,
                $"queues/%2F/{PaymentMessagingNames.InventoryPaymentFailedQueue}");
            Assert.True(inventoryFailedQueueDocument.RootElement.GetProperty("durable").GetBoolean());
            Assert.False(inventoryFailedQueueDocument.RootElement.GetProperty("auto_delete").GetBoolean());

            Assert.Contains(bindings.RootElement.EnumerateArray(), binding =>
                binding.GetProperty("source").GetString()
                    == PaymentMessagingNames.PaymentFailedExchange
                && binding.GetProperty("destination").GetString()
                    == PaymentMessagingNames.InventoryPaymentFailedQueue
                && binding.GetProperty("destination_type").GetString() == "exchange");

            using var orderQueueDocument = await GetJsonAsync(
                client,
                $"queues/%2F/{PaymentMessagingNames.OrderPaymentSucceededQueue}");
            Assert.True(orderQueueDocument.RootElement.GetProperty("durable").GetBoolean());
            Assert.False(orderQueueDocument.RootElement.GetProperty("auto_delete").GetBoolean());

            Assert.Contains(bindings.RootElement.EnumerateArray(), binding =>
                binding.GetProperty("source").GetString()
                    == PaymentMessagingNames.PaymentSucceededExchange
                && binding.GetProperty("destination").GetString()
                    == PaymentMessagingNames.OrderPaymentSucceededQueue
                && binding.GetProperty("destination_type").GetString() == "exchange");

            using var faultQueueDocument = await GetJsonAsync(
                client,
                $"queues/%2F/{PaymentMessagingNames.PaymentProcessingFaultsQueue}");
            Assert.True(faultQueueDocument.RootElement.GetProperty("durable").GetBoolean());
            Assert.False(faultQueueDocument.RootElement.GetProperty("auto_delete").GetBoolean());

            Assert.Contains(bindings.RootElement.EnumerateArray(), binding =>
            {
                var source = binding.GetProperty("source").GetString();
                return source?.Contains("MassTransit:Fault", StringComparison.Ordinal) == true
                    && source.Contains("PaymentSucceededEvent", StringComparison.Ordinal)
                    && binding.GetProperty("destination").GetString()
                        == PaymentMessagingNames.PaymentProcessingFaultsQueue
                    && binding.GetProperty("destination_type").GetString() == "exchange";
            });
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Publish_WhenPaymentSucceededConsumersFail_LogsCentralFaultsWithoutSourceErrorQueues()
    {
        var logs = new FaultLogSink();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(logs));
        services.AddMessaging(CreateConfiguration());
        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        var message = CreatePaymentSucceededEvent();

        await bus.StartAsync(CancellationToken.None);
        try
        {
            await bus.Publish(message, CancellationToken.None);

            var faultLogs = await logs.ReadForEventAsync(
                message.EventId,
                expectedCount: 3,
                TimeSpan.FromSeconds(30));

            Assert.Equal(
                ["Finance", "Inventory", "Order"],
                faultLogs
                    .Select(log => Assert.IsType<string>(log["Component"]))
                    .OrderBy(component => component)
                    .ToArray());

            using var client = CreateManagementClient();
            await AssertQueueDoesNotExistAsync(
                client,
                PaymentMessagingNames.FinancePaymentSucceededQueue + "_error");
            await AssertQueueDoesNotExistAsync(
                client,
                PaymentMessagingNames.InventoryPaymentSucceededQueue + "_error");
            await AssertQueueDoesNotExistAsync(
                client,
                PaymentMessagingNames.OrderPaymentSucceededQueue + "_error");
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None);
        }
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = Environment.GetEnvironmentVariable("HAGGLY_TEST_RABBITMQ_HOST")
                    ?? "localhost",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = Environment.GetEnvironmentVariable("HAGGLY_TEST_RABBITMQ_USERNAME")
                    ?? "guest",
                ["RabbitMq:Password"] = Environment.GetEnvironmentVariable("HAGGLY_TEST_RABBITMQ_PASSWORD")
                    ?? "guest",
                ["Outbox:Enabled"] = "false"
            })
            .Build();

    private static HttpClient CreateManagementClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(
                Environment.GetEnvironmentVariable("HAGGLY_TEST_RABBITMQ_MANAGEMENT_URL")
                ?? "http://localhost:15672/api/")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                $"{Environment.GetEnvironmentVariable("HAGGLY_TEST_RABBITMQ_USERNAME") ?? "guest"}:" +
                $"{Environment.GetEnvironmentVariable("HAGGLY_TEST_RABBITMQ_PASSWORD") ?? "guest"}")));
        return client;
    }

    private static PaymentSucceededEvent CreatePaymentSucceededEvent()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        return new PaymentSucceededEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            occurredAt,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            300_000m,
            "VND",
            "provider-transaction-boundary-test",
            [Guid.NewGuid()]);
    }

    private static async Task AssertQueueDoesNotExistAsync(
        HttpClient client,
        string queueName)
    {
        using var response = await client.GetAsync($"queues/%2F/{queueName}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(content);
    }

    private sealed class FaultLogSink : ILoggerProvider
    {
        private readonly Channel<IReadOnlyDictionary<string, object?>> entries
            = Channel.CreateUnbounded<IReadOnlyDictionary<string, object?>>();

        public ILogger CreateLogger(string categoryName) => new FaultLogger(entries.Writer);

        public void Dispose()
        {
        }

        public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadForEventAsync(
            Guid eventId,
            int expectedCount,
            TimeSpan timeout)
        {
            using var cancellation = new CancellationTokenSource(timeout);
            var matches = new List<IReadOnlyDictionary<string, object?>>(expectedCount);

            while (matches.Count < expectedCount)
            {
                var entry = await entries.Reader.ReadAsync(cancellation.Token);
                if (entry.TryGetValue("EventId", out var value)
                    && value is Guid loggedEventId
                    && loggedEventId == eventId)
                {
                    matches.Add(entry);
                }
            }

            return matches;
        }

        private sealed class FaultLogger(
            ChannelWriter<IReadOnlyDictionary<string, object?>> writer) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
                => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel != LogLevel.Error
                    || state is not IEnumerable<KeyValuePair<string, object?>> properties)
                {
                    return;
                }

                writer.TryWrite(properties.ToDictionary(item => item.Key, item => item.Value));
            }
        }
    }
}
