using System.Diagnostics;
using Haggly.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxBackgroundService> logger,
    IOptions<OutboxBenchmarkOptions>? benchmarkOptions = null) : BackgroundService
{
    private readonly OutboxOptions settings = options.Value;
    private readonly OutboxBenchmarkOptions benchmarkSettings =
        benchmarkOptions?.Value ?? new OutboxBenchmarkOptions();
    
    private int _totalProcessedCount = 0;
    private int _totalIterations = 0;
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
          return;

        if (!benchmarkSettings.Enabled)
        {
          try
          {
            await ProcessOutboxMessage(stoppingToken);
          }
          catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
          {
            OutboxLoggers.LogOperationCancelled(logger);
          }
          catch (Exception ex)
          {
            OutboxLoggers.LogError(logger, ex);
          }

          return;
        }
        
        // Benchmark measuring (how many events can be handled per minute)
        # region 
        OutboxLoggers.LogStarting(logger);

        var stopwatch = Stopwatch.StartNew();
        using var benchmarkCts = new CancellationTokenSource(benchmarkSettings.Duration);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(benchmarkCts.Token, stoppingToken);

        try
        {
          await ProcessOutboxMessage(linkedCts.Token);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
          OutboxLoggers.LogOperationCancelled(logger);
        }
        catch (Exception ex)
        {
          OutboxLoggers.LogError(logger, ex);
        }

        stopwatch.Stop();
        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var messagesPerSecond = elapsedSeconds > 0 ? _totalProcessedCount / elapsedSeconds : 0;
        OutboxLoggers.LogFinished(logger, _totalIterations, _totalProcessedCount);
        OutboxLoggers.LogBenchmarkCompleted(logger, elapsedSeconds, _totalProcessedCount, messagesPerSecond);
        
        #endregion
    }

    private async Task ProcessOutboxMessage(CancellationToken cancellationToken)
    {
      using var scope = scopeFactory.CreateScope();
      var outboxProcessor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

      while (!cancellationToken.IsCancellationRequested)
      {
        var iterationCount = Interlocked.Increment(ref  _totalIterations);
        
        int processedMessages = await outboxProcessor.ProcessPendingAsync(settings.BatchSize, cancellationToken);
            
        var totalProcessedCount = Interlocked.Add(ref _totalProcessedCount, processedMessages);
        if (benchmarkSettings.Enabled)
          OutboxLoggers.LogIterationCompleted(logger, iterationCount, processedMessages, totalProcessedCount);

        if (processedMessages == 0)
          await Task.Delay(settings.Interval, cancellationToken);
      }
    }
}
 
internal static partial class OutboxLoggers
{
  [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService starting...")]
  internal static partial void LogStarting(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Starting iteration {IterationCount}")]
  internal static partial void LogStartingIteration(ILogger logger, int iterationCount);

  [LoggerMessage(Level = LogLevel.Information, Message = "Iteration {IterationCount} completed. Processed {ProcessedMessages} messages. Total processed: {TotalProcessedMessages}")]
  internal static partial void LogIterationCompleted(ILogger logger, int iterationCount, int processedMessages, int totalProcessedMessages);

  [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService operation cancelled.")]
  internal static partial void LogOperationCancelled(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred in OutboxBackgroundService")]
  internal static partial void LogError(ILogger logger, Exception exception);

  [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService finished. Total iterations: {IterationCount}, Total processed messages: {TotalProcessedMessages}")]
  internal static partial void LogFinished(ILogger logger, int iterationCount, int totalProcessedMessages);

  [LoggerMessage(Level = LogLevel.Information, Message = "Outbox benchmark completed. Window: {DurationSeconds:F0}s, successfully processed: {ProcessedMessages}, throughput: {MessagesPerSecond:F2} messages/s")]
  internal static partial void LogBenchmarkCompleted(ILogger logger, double durationSeconds, int processedMessages, double messagesPerSecond);
  [LoggerMessage(Level = LogLevel.Information, Message = "Outbox processing completed. Total time: {TotalTime}ms, Query time: {QueryTime}ms, Publish time: {PublishTime}ms, Update time: {UpdateTime}ms, Messages processed: {MessageCount}")]
  internal static partial void LogProcessingPerformance(ILogger logger, long totalTime, long queryTime, long publishTime, long updateTime, int messageCount);
}
