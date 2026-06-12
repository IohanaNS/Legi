// Application/Auth/Commands/Login/LoginCommandValidator.cs

using FluentValidation;

namespace Legi.Identity.Application.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required")
            .MaximumLength(255).WithMessage("Email or username must be at most 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
