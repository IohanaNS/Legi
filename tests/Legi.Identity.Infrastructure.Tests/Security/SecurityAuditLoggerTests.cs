using Legi.Identity.Application.Common.Models;
using Legi.Identity.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class SecurityAuditLoggerTests
{
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public readonly List<(LogLevel Level, EventId EventId, string Message)> Entries = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public void Record_EmitsInformation_WithStableEventIdAndStructuredFields()
    {
        var logger = new CapturingLogger<SecurityAuditLogger>();
        var sut = new SecurityAuditLogger(logger);

        sut.Record(new SecurityAuditEvent(
            SecurityEventType.LoginFailed,
            Identifier: "alice@example.com",
            IpAddress: "203.0.113.7",
            Detail: "invalid-password"));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        // EventId is stable per event type so a log sink can filter/alert on it.
        Assert.Equal((int)SecurityEventType.LoginFailed, entry.EventId.Id);
        Assert.Equal(nameof(SecurityEventType.LoginFailed), entry.EventId.Name);
        Assert.Contains("LoginFailed", entry.Message);
        Assert.Contains("alice@example.com", entry.Message);
        Assert.Contains("invalid-password", entry.Message);
    }
}
