using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Legi.Messaging.Outbox;

/// <summary>
/// Periodically prunes processed outbox rows and consumed inbox rows older than
/// <see cref="OutboxOptions.RetentionDays"/> (Fase 6 6D.2), so the tables don't
/// grow unbounded. Poison outbox rows are kept (see <see cref="RetentionCleaner"/>).
///
/// Generic over <typeparamref name="TContext"/> so each service prunes its own DB.
/// Coarse interval — cleanup is cheap and not latency-sensitive.
/// </summary>
public class RetentionCleanupWorker<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<RetentionCleanupWorker<TContext>> _logger;

    public RetentionCleanupWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<RetentionCleanupWorker<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.RetentionIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTime.UtcNow - TimeSpan.FromDays(_options.RetentionDays);

                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<TContext>();
                var (outboxDeleted, inboxDeleted) = await RetentionCleaner.CleanupAsync(
                    ctx, cutoff, stoppingToken);

                if (outboxDeleted > 0 || inboxDeleted > 0)
                    _logger.LogInformation(
                        "Retention cleanup for {Context}: pruned {Outbox} outbox + {Inbox} inbox row(s) older than {Days}d",
                        typeof(TContext).Name, outboxDeleted, inboxDeleted, _options.RetentionDays);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Retention cleanup cycle failed for {Context}; will retry next interval",
                    typeof(TContext).Name);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
