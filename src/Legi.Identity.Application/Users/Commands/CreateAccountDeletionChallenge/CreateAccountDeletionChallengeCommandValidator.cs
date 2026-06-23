using FluentValidation;

namespace Legi.Identity.Application.Users.Commands.CreateAccountDeletionChallenge;

public class CreateAccountDeletionChallengeCommandValidator
    : AbstractValidator<CreateAccountDeletionChallengeCommand>
{
    public CreateAccountDeletionChallengeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}
