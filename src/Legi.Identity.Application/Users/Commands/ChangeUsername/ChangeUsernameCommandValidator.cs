using FluentValidation;

namespace Legi.Identity.Application.Users.Commands.ChangeUsername;

public class ChangeUsernameCommandValidator : AbstractValidator<ChangeUsernameCommand>
{
    public ChangeUsernameCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.NewUsername)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(30).WithMessage("Username must be at most 30 characters")
            .Matches(@"^[a-z][a-z0-9_]*$").WithMessage("Username must start with a letter and contain only letters, numbers and underscore");

        RuleFor(x => x.ChallengeToken).NotEmpty().WithMessage("Challenge token is required");
    }
}
