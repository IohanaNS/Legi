using FluentValidation;

namespace Legi.Identity.Application.Users.Commands.SendAccountDeletionEmailCode;

public class SendAccountDeletionEmailCodeCommandValidator
    : AbstractValidator<SendAccountDeletionEmailCodeCommand>
{
    public SendAccountDeletionEmailCodeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}
