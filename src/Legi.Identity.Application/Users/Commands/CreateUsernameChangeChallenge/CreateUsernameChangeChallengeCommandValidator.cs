using FluentValidation;

namespace Legi.Identity.Application.Users.Commands.CreateUsernameChangeChallenge;

public class CreateUsernameChangeChallengeCommandValidator
    : AbstractValidator<CreateUsernameChangeChallengeCommand>
{
    public CreateUsernameChangeChallengeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}
