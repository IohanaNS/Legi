namespace Legi.Messaging.RabbitMq;

/// <summary>
/// Connection settings for RabbitMQ. Bound from the "RabbitMq" section of
/// the service's appsettings.json.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.3.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    /// <summary>
    /// Friendly name reported to the broker for diagnostics. Defaults to the
    /// process's main module name; override to distinguish multiple instances.
    /// </summary>
    public string? ClientProvidedName { get; set; }
}