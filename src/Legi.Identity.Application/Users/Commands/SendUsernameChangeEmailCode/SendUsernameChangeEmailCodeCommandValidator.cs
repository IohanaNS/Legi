using FluentValidation;

namespace Legi.Identity.Application.Users.Commands.SendUsernameChangeEmailCode;

public class SendUsernameChangeEmailCodeCommandValidator
    : AbstractValidator<SendUsernameChangeEmailCodeCommand>
{
    public SendUsernameChangeEmailCodeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
