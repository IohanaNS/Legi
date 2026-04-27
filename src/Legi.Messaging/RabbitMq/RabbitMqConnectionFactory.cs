using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Legi.Messaging.RabbitMq;

/// <summary>
/// Owns the single <see cref="IConnection"/> for a service. The connection
/// is established lazily on the first request and reused for the lifetime of
/// the process. Automatic recovery is enabled, so transient broker outages
/// are healed without intervention.
/// 
/// Thread safety: connection acquisition is guarded by a SemaphoreSlim so
/// concurrent first-time callers cooperate; subsequent calls return the
/// cached connection without locking.
/// 
/// Channel creation is the caller's responsibility — channels are not
/// thread-safe and should be scoped to a single operation or consumer host.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.3.
/// </summary>
public class RabbitMqConnectionFactory : IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns the shared connection, opening it on first call.
    /// </summary>
    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            // The previous connection may exist but be closed (rare; recovery
            // usually heals it). Dispose before replacing to release resources.
            if (_connection is not null)
                await _connection.DisposeAsync();

            _connection = await CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            VirtualHost = _settings.VirtualHost,
            UserName = _settings.Username,
            Password = _settings.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = _settings.ClientProvidedName,
        };

        _logger.LogInformation(
            "Opening RabbitMQ connection to {Host}:{Port} (vhost: {VirtualHost})",
            _settings.Host, _settings.Port, _settings.VirtualHost);

        var connection = await factory.CreateConnectionAsync(cancellationToken);

        _logger.LogInformation("RabbitMQ connection established");

        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }
}