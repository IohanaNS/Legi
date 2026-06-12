using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Common.Behaviors;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var logger = new CapturingLogger<LoggingBehavior<TestRequest, string>>();
        var behavior = new LoggingBehavior<TestRequest, string>(logger);
        var request = new TestRequest("value");
        var nextCalled = false;

        // Act
        var response = await behavior.Handle(
            request,
            () =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("ok", response);
    }

    [Fact]
    public async Task Handle_ShouldNotLogSensitiveRequestPayload()
    {
        // Arrange
        var logger = new CapturingLogger<LoggingBehavior<LoginCommand, LoginResponse>>();
        var behavior = new LoggingBehavior<LoginCommand, LoginResponse>(logger);
        var request = new LoginCommand("reader@example.com", "SuperSecret123!");

        // Act
        await behavior.Handle(
            request,
            () => Task.FromResult(new LoginResponse(
                Guid.NewGuid(),
                "reader@example.com",
                "reader",
                "access-token",
                "refresh-token",
                DateTime.UtcNow.AddMinutes(15),
                DateTime.UtcNow.AddDays(7))),
            CancellationToken.None);

        // Assert
        var logs = string.Join(Environment.NewLine, logger.Messages);
        Assert.Contains("LoginCommand", logs);
        Assert.DoesNotContain("SuperSecret123!", logs);
        Assert.DoesNotContain("reader@example.com", logs);
        Assert.DoesNotContain("Password", logs);
        Assert.DoesNotContain("EmailOrUsername", logs);
    }

    private sealed record TestRequest(string Value) : IRequest<string>;

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
