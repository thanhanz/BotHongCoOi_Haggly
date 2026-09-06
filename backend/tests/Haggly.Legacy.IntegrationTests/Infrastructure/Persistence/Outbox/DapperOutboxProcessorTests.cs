using Dapper;
using Haggly.Application.Common.Messaging;
using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Outbox;

public sealed class DapperOutboxProcessorTests
{
    [Fact]
    public async Task WriteAsync_WhenTransactionCommits_PersistsOutboxMessage()
    {
        var domainEvent = CreateEvent();
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var writer = CreateWriter(dbContext);

        await writer.WriteAsync(domainEvent, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        var stored = await connection.QuerySingleAsync<StoredOutboxMessage>(
            """
            SELECT "Id", "EventType", "Payload"::text AS "Payload",
                   "CorrelationId", "OccurredAt", "CreatedAt", "ProcessedAt", "ErrorMessage"
            FROM messaging.outbox_messages
            WHERE "CorrelationId" = @CorrelationId;
            """,
            new { domainEvent.CorrelationId });

        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal("tests.outbox-event.v1", stored.EventType);
        Assert.Equal(domainEvent.CorrelationId, stored.CorrelationId);
        Assert.True(
            (domainEvent.OccurredAt.UtcDateTime - stored.OccurredAt.ToUniversalTime()).Duration()
            < TimeSpan.FromMicroseconds(1));
        Assert.NotEqual(default, stored.CreatedAt);
        Assert.Null(stored.ProcessedAt);
        Assert.Contains(domainEvent.Value, stored.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteAsync_WhenTransactionRollsBack_DoesNotPersistOutboxMessage()
    {
        var domainEvent = CreateEvent();
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var writer = CreateWriter(dbContext);

        await writer.WriteAsync(domainEvent, CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);

        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM messaging.outbox_messages
            WHERE "CorrelationId" = @CorrelationId;
            """,
            new { domainEvent.CorrelationId });

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WriteAsync_WithoutActiveTransaction_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var writer = CreateWriter(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            writer.WriteAsync(CreateEvent(), CancellationToken.None));

        Assert.Contains("active database transaction", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenPublishSucceeds_SetsProcessedAt()
    {
        var domainEvent = CreateEvent();
        var publisher = new RecordingDomainEventPublisher();
        await InsertOutboxMessageAsync(domainEvent);
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext, publisher);

        var processedCount = await processor.ProcessPendingAsync(10, CancellationToken.None);

        var stored = await ReadOutboxMessageAsync(domainEvent.CorrelationId);
        Assert.Contains(domainEvent, publisher.PublishedEvents.OfType<TestOutboxEvent>());
        Assert.Equal(publisher.PublishedEvents.Count, processedCount);
        Assert.NotNull(stored.ProcessedAt);
        Assert.Null(stored.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenPublishFails_StoresErrorAndLeavesMessagePending()
    {
        var domainEvent = CreateEvent();
        var publisher = new RecordingDomainEventPublisher(new InvalidOperationException("broker unavailable"));
        await InsertOutboxMessageAsync(domainEvent);
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext, publisher);

        var processedCount = await processor.ProcessPendingAsync(10, CancellationToken.None);

        var stored = await ReadOutboxMessageAsync(domainEvent.CorrelationId);
        Assert.Equal(0, processedCount);
        Assert.Null(stored.ProcessedAt);
        Assert.Contains("broker unavailable", stored.ErrorMessage, StringComparison.Ordinal);
    }

    private static DapperOutboxProcessor CreateProcessor(
        HagglyDbContext dbContext,
        IDomainEventPublisher? publisher = null)
        => new(
            dbContext,
            new DomainEventTypeRegistry(
            [
                DomainEventTypeRegistration.For<TestOutboxEvent>("tests.outbox-event.v1")
            ]),
            publisher ?? new RecordingDomainEventPublisher(),
            TimeProvider.System);

    private static DapperOutboxWriter CreateWriter(HagglyDbContext dbContext)
        => new(
            dbContext,
            new DomainEventTypeRegistry(
            [
                DomainEventTypeRegistration.For<TestOutboxEvent>("tests.outbox-event.v1")
            ]),
            TimeProvider.System);

    private static async Task InsertOutboxMessageAsync(TestOutboxEvent domainEvent)
    {
        await using (var cleanupConnection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None))
        {
            await cleanupConnection.ExecuteAsync(
                """
                UPDATE messaging.outbox_messages
                SET "ProcessedAt" = COALESCE("ProcessedAt", @ProcessedAt)
                WHERE "ProcessedAt" IS NULL;
                """,
                new { ProcessedAt = DateTimeOffset.UtcNow });
        }

        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await CreateWriter(dbContext).WriteAsync(domainEvent, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    private static async Task<StoredOutboxMessage> ReadOutboxMessageAsync(Guid correlationId)
    {
        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        return await connection.QuerySingleAsync<StoredOutboxMessage>(
            """
            SELECT "Id", "EventType", "Payload"::text AS "Payload",
                   "CorrelationId", "OccurredAt", "CreatedAt", "ProcessedAt", "ErrorMessage"
            FROM messaging.outbox_messages
            WHERE "CorrelationId" = @CorrelationId;
            """,
            new { CorrelationId = correlationId });
    }

    private static HagglyDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(IntegrationTestDatabase.ConnectionString)
            .Options);

    private static TestOutboxEvent CreateEvent()
        => new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "outbox-payload");

    private sealed record TestOutboxEvent(
        Guid EventId,
        Guid CorrelationId,
        DateTimeOffset OccurredAt,
        string Value) : IDomainEvent;

    private sealed record StoredOutboxMessage(
        Guid Id,
        string EventType,
        string Payload,
        Guid CorrelationId,
        DateTime OccurredAt,
        DateTime CreatedAt,
        DateTime? ProcessedAt,
        string? ErrorMessage);

    private sealed class RecordingDomainEventPublisher(Exception? exception = null)
        : IDomainEventPublisher
    {
        public List<IDomainEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            if (exception is not null)
                throw exception;

            PublishedEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
