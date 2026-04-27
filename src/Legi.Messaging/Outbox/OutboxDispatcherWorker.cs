using Legi.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Legi.Messaging.Outbox;

/// <summary>
/// Background service that polls the outbox table, publishes pending messages
/// to RabbitMQ via <see cref="IRabbitMqPublisher"/>, and marks rows processed
/// once the broker confirms receipt. One instance per service; multiple
/// instances cooperate safely via <c>SELECT ... FOR UPDATE SKIP LOCKED</c>.
/// 
/// Generic over the service's DbContext type so each service registers its own
/// dispatcher pointed at its own outbox table.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.3, 8.2, and section 7.5.
/// </summary>
public class OutboxDispatcherWorker<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxDispatcherWorker<TContext>> _logger;

    public OutboxDispatcherWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcherWorker<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox dispatcher started for {Context}: polling every {IntervalMs}ms, batch size {BatchSize}, max attempts {MaxAttempts}",
            typeof(TContext).Name, _options.PollingIntervalMs, _options.BatchSize, _options.MaxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndDispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — exit the loop.
                break;
            }
            catch (Exception ex)
            {
                // Catch-all so a transient failure (DB blip, broker outage)
                // does not kill the worker. Sleep and retry on the next cycle.
                _logger.LogError(ex,
                    "Outbox dispatcher cycle failed for {Context}; will retry next poll",
                    typeof(TContext).Name);
            }

            try
            {
                await Task.Delay(_options.PollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox dispatcher stopped for {Context}", typeof(TContext).Name);
    }

    private async Task PollAndDispatchBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        // The transaction holds the row locks acquired by FOR UPDATE SKIP LOCKED.
        // It always commits — failures are handled per-row by updating Attempts
        // and NextRetryAt, not by rolling back the whole batch.
        await using var transaction = await ctx.Database.BeginTransactionAsync(stoppingToken);

        var batch = await FetchBatchAsync(ctx, stoppingToken);

        if (batch.Count == 0)
        {
            await transaction.CommitAsync(stoppingToken);
            return;
        }

        _logger.LogDebug(
            "Outbox dispatcher claimed batch of {Count} message(s) for {Context}",
            batch.Count, typeof(TContext).Name);

        var succeeded = 0;
        var failed = 0;

        foreach (var message in batch)
        {
            // We deliberately pass stoppingToken to the publish; if shutdown
            // is requested, in-flight publishes are cancelled and the row
            // stays pending (Attempts unchanged, picked up next startup).
            var success = await TryPublishAsync(message, publisher, stoppingToken);
            if (success) succeeded++; else failed++;
        }

        // Persist row updates with CancellationToken.None: even if shutdown was
        // requested mid-batch, we MUST record the state of messages we already
        // sent — the broker has them. Losing the ProcessedAt update would cause
        // duplicate publishes on next startup.
        await ctx.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        _logger.LogInformation(
            "Outbox batch dispatched for {Context}: {Succeeded} succeeded, {Failed} failed",
            typeof(TContext).Name, succeeded, failed);
    }

    private async Task<List<OutboxMessage>> FetchBatchAsync(
        TContext ctx,
        CancellationToken cancellationToken)
    {
        // Raw SQL because EF LINQ does not expose FOR UPDATE SKIP LOCKED.
        // The query:
        //   - filters rows that have not been published yet
        //   - filters rows still within their retry budget
        //   - filters rows whose retry window has come due
        //   - orders by OccurredAt for FIFO
        //   - locks the claimed rows so concurrent dispatchers skip them
        //
        // Identifiers are quoted to match the default Npgsql PascalCase
        // casing used elsewhere in the project.
        var sql = """
            SELECT * FROM "OutboxMessages"
            WHERE "ProcessedAt" IS NULL
              AND "Attempts" < {0}
              AND "NextRetryAt" <= NOW()
            ORDER BY "OccurredAt"
            LIMIT {1}
            FOR UPDATE SKIP LOCKED
            """;

        return await ctx.Set<OutboxMessage>()
            .FromSqlRaw(sql, _options.MaxAttempts, _options.BatchSize)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> TryPublishAsync(
        OutboxMessage message,
        IRabbitMqPublisher publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(
                message.Type,
                message.Payload,
                message.Id,
                message.OccurredAt,
                cancellationToken);

            // Broker has confirmed durable receipt — safe to mark processed.
            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown requested. Do not increment Attempts — this is not a
            // genuine failure. The row stays pending and is picked up later.
            throw;
        }
        catch (Exception ex)
        {
            message.Attempts++;
            message.Error = ex.Message;

            if (message.Attempts >= _options.MaxAttempts)
            {
                // Poison: stop scheduling retries. NextRetryAt does not need
                // updating — the Attempts < MaxAttempts filter excludes the row.
                _logger.LogError(ex,
                    "Outbox message {MessageId} of type {Type} reached max attempts ({MaxAttempts}); marking poison",
                    message.Id, message.Type, _options.MaxAttempts);
            }
            else
            {
                var backoff = ComputeBackoff(message.Attempts);
                message.NextRetryAt = DateTime.UtcNow.Add(backoff);

                _logger.LogWarning(ex,
                    "Outbox message {MessageId} publish failed (attempt {Attempts}/{MaxAttempts}); next retry in {BackoffSeconds}s",
                    message.Id, message.Attempts, _options.MaxAttempts, backoff.TotalSeconds);
            }

            return false;
        }
    }

    /// <summary>
    /// Backoff schedule by attempt count. Index is the just-incremented
    /// <see cref="OutboxMessage.Attempts"/>, so attempt 1 is the second try.
    /// Schedule: 1s, 5s, 30s, 60s for attempts 1-4. After the 5th attempt we
    /// give up; the row is marked poison and this method is not called.
    /// </summary>
    private static TimeSpan ComputeBackoff(int attempts) => attempts switch
    {
        1 => TimeSpan.FromSeconds(1),
        2 => TimeSpan.FromSeconds(5),
        3 => TimeSpan.FromSeconds(30),
        4 => TimeSpan.FromSeconds(60),
        _ => TimeSpan.FromSeconds(60),
    };
}