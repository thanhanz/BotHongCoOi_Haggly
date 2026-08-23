using System.Net.Http.Headers;
using System.Text.Json;
using Haggly.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(content);
    }
}
