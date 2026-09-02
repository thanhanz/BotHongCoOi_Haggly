using Haggly.Application.Common.Messaging;
using Haggly.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Messaging;

public sealed class OutboxBackgroundServiceTests
{
    [Fact]
    public async Task ProcessOnceAsync_WhenCalled_ProcessesConfiguredBatchInScope()
    {
        var processor = new RecordingOutboxProcessor();
        var services = new ServiceCollection()
            .AddScoped<IOutboxProcessor>(_ => processor)
            .BuildServiceProvider();
        var service = new OutboxBackgroundService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions
            {
                BatchSize = 25,
                Interval = TimeSpan.FromSeconds(5)
            }),
            NullLogger<OutboxBackgroundService>.Instance);

        await service.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(25, processor.BatchSize);
        Assert.Equal(1, processor.CallCount);
    }

    private sealed class RecordingOutboxProcessor : IOutboxProcessor
    {
        public int BatchSize { get; private set; }
        public int CallCount { get; private set; }

        public Task<int> ProcessPendingAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            BatchSize = batchSize;
            CallCount++;
            return Task.FromResult(0);
        }
    }
}
