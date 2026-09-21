using System.Collections.Concurrent;
using System.Diagnostics;
using Dapper;
using Haggly.Application.Common.Messaging;
using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class DapperOutboxProcessor(
    HagglyDbContext dbContext,
    DomainEventTypeRegistry eventTypes,
    IDomainEventPublisher publisher,
    ILogger<DapperOutboxProcessor> logger,
    IOptions<OutboxBenchmarkOptions>? benchmarkOptions = null) : IOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OutboxBenchmarkOptions benchmarkSettings =
        benchmarkOptions?.Value ?? new OutboxBenchmarkOptions();
    
    private const string QueryUnprocessedEventsSql =
        """
        SELECT "Id", "EventType", "Payload"::text AS "Payload"
        FROM messaging.outbox_messages
        WHERE "ProcessedAt" IS NULL
        ORDER BY "OccurredAt", "Id"
        LIMIT @BatchSize;
        """;

    private const string UpdateProcessedSql =
        """
        UPDATE messaging.outbox_messages
        SET "ProcessedAt" = v.ProcessedAt,
            "ErrorMessage" = v.ErrorMessage
        FROM (VALUES
            {0}
        ) AS v(Id, ProcessedAt, ErrorMessage)
        WHERE "Id" = v.Id::uuid;
        """;
    
    //TODO: This function need to optimize about the performance
    // (Include: QueryTime, PublishEventsTime, UpdateTime, BatchSize, process run parallel)
    public async Task<int> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        var connection = dbContext.Database.GetDbConnection();
        
        // Measuring each processing step using StopWatch
        var totalStopWatch = benchmarkSettings.Enabled
          ? Stopwatch.StartNew()
          : null;
        var stepStopWatch = benchmarkSettings.Enabled ? new Stopwatch() : null;
        
        var updatedQueue = new ConcurrentQueue<OutboxUpdate>();
        var processedCount = 0;

        // Query unprocessed events
        stepStopWatch?.Restart();
        
        var messages = await connection.QueryAsync<PendingOutboxMessage>(
          new CommandDefinition(
          QueryUnprocessedEventsSql,
            new { BatchSize = batchSize },
            cancellationToken: cancellationToken));
      
        var queryTime = stepStopWatch?.ElapsedMilliseconds ?? 0;
        // Publish Stage
        stepStopWatch?.Restart();
        
        var publishTasks = messages
          .Select(message => PublishMessage(
            message,
            updatedQueue,
            publisher,
            eventTypes,
            cancellationToken))
          .ToList();
        
        // Run parallel
        await Task.WhenAll(publishTasks);
        
        var publishTime = stepStopWatch?.ElapsedMilliseconds ?? 0;
        
        //Make sure events stored in database be published AND updated the ProcessedAt field
        // 12:00:59.999  Message successfully published to RabbitMQ
        // 12:01:00.000  Benchmark token is canceled
        // 12:01:00.001  Need to update ProcessedAt in PostgreSQL
        using var persistenceCts = cancellationToken.IsCancellationRequested
          ? new CancellationTokenSource(TimeSpan.FromSeconds(10))
          : null;
        
        var persistenceToken = persistenceCts?.Token ?? cancellationToken;
        
        stepStopWatch?.Restart();
        
        // Update stage
        if (!updatedQueue.IsEmpty)
        {
          var updates = updatedQueue.ToList();
          var valuesList = string.Join(",",
            updates.Select((_, index) => $"(@Id{index}, @ProcessedAt{index}, @ErrorMessage{index})"));
          
          var parameters = new DynamicParameters();

          for (int i = 0; i < updates.Count; i++)
          {
            parameters.Add($"@Id{i}", updates[i].Id.ToString());
            parameters.Add($"@ProcessedAt{i}", updates[i].ProcessedAt);
            parameters.Add($"@ErrorMessage{i}", updates[i].ErrorMessage);

            if (updates[i].ProcessedAt is not null && updates[i].ErrorMessage is null)
              processedCount++;
          }
          
          var formattedSql = string.Format(UpdateProcessedSql, valuesList);
          
          await connection.ExecuteAsync(new CommandDefinition(
            formattedSql,
            parameters,
            cancellationToken: persistenceToken));
        }
        
        var updateTime = stepStopWatch?.ElapsedMilliseconds ?? 0;
        
        if (benchmarkSettings.Enabled)
        {
          totalStopWatch!.Stop();
          var totalTime = totalStopWatch.ElapsedMilliseconds;
          OutboxLoggers.LogProcessingPerformance(logger, totalTime, queryTime, publishTime, updateTime, processedCount);
        }

        return processedCount;
    }

    private static async Task PublishMessage(
      PendingOutboxMessage message, 
      ConcurrentQueue<OutboxUpdate> updatedQueue,
      IDomainEventPublisher publisher,
      DomainEventTypeRegistry eventTypes,
      CancellationToken cancellationToken)
    {
      try
      {
        var outEventType = eventTypes.GetClrType(message.EventType);
                
        var domainEvent = JsonSerializer.Deserialize(message.Payload, outEventType, JsonOptions) as IDomainEvent
                          ?? throw new JsonException(
                            $"Payload for '{message.EventType}' could not be deserialized as a domain event.");

        await publisher.PublishAsync(domainEvent, cancellationToken);

        updatedQueue.Enqueue(new OutboxUpdate{ Id = message.Id, ProcessedAt = DateTime.UtcNow });
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        // The benchmark or host is stopping. Leave the message pending so it can be retried.
      }
      catch (Exception exception)
      {
        var errorMessage = exception.ToString();
        updatedQueue.Enqueue(new OutboxUpdate
        { 
            Id = message.Id, 
            ErrorMessage = errorMessage.Length <= 2000
              ? errorMessage
              : errorMessage[..2000]
        });
      }
    }

    private struct OutboxUpdate
    {
      public Guid Id { get; init; }
      public DateTimeOffset? ProcessedAt { get; init; }
      public string? ErrorMessage  { get; init; }
    }
    
    private sealed record PendingOutboxMessage(Guid Id, string EventType, string Payload);
}
