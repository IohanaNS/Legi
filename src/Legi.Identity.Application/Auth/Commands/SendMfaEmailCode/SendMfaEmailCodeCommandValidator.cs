using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.SendMfaEmailCode;

public class SendMfaEmailCodeCommandValidator : AbstractValidator<SendMfaEmailCodeCommand>
{
    public SendMfaEmailCodeCommandValidator()
    {
        RuleFor(x => x.MfaToken).NotEmpty().WithMessage("MFA challenge token is required");
    }
}
