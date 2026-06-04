using Legi.Catalog.Application.Common.Behaviors;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging.Abstractions;

namespace Legi.Catalog.Application.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestRequest, string>(
            NullLogger<LoggingBehavior<TestRequest, string>>.Instance);

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

    private sealed record TestRequest(string Value) : IRequest<string>;
}
