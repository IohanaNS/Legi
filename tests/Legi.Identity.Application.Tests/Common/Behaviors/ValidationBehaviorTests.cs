using FluentValidation;
using Legi.Identity.Application.Common.Behaviors;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenThereAreNoValidators()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var request = new TestRequest("valid");
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
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest(string.Empty);
        var nextCalled = false;

        // Act
        var act = async () => await behavior.Handle(
            request,
            () =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(act);
        Assert.False(nextCalled);
    }

    private sealed record TestRequest(string Value) : IRequest<string>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty();
        }
    }
}
