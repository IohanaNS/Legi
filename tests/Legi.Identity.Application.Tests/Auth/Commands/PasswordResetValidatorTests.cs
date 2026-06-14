using Legi.Identity.Application.Auth.Commands.ForgotPassword;
using Legi.Identity.Application.Auth.Commands.ResetPassword;

namespace Legi.Identity.Application.Tests.Auth.Commands;

public class PasswordResetValidatorTests
{
    private readonly ForgotPasswordCommandValidator _forgotValidator = new();
    private readonly ResetPasswordCommandValidator _resetValidator = new();

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("", false)]
    [InlineData("not-an-email", false)]
    public void ForgotPassword_ValidatesEmail(string email, bool expectedValid)
    {
        var result = _forgotValidator.Validate(new ForgotPasswordCommand(email));
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("tok", "ValidPass1", true)]
    [InlineData("", "ValidPass1", false)]           // missing token
    [InlineData("tok", "short1", false)]            // too short
    [InlineData("tok", "alllowercase1", false)]     // no uppercase
    [InlineData("tok", "NoDigitsHere", false)]      // no number
    public void ResetPassword_ValidatesTokenAndPassword(string token, string password, bool expectedValid)
    {
        var result = _resetValidator.Validate(new ResetPasswordCommand(token, password));
        Assert.Equal(expectedValid, result.IsValid);
    }
}
