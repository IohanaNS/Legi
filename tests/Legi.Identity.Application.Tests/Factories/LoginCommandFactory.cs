using Legi.Identity.Application.Auth.Commands.Login;

namespace Legi.Identity.Application.Tests.Factories;

public static class LoginCommandFactory
{
    public static LoginCommand Create(
        string? emailOrUsername = null,
        string? password = null)
    {
        return new LoginCommand(
            emailOrUsername ?? "teste@exemplo.com",
            password ?? "Senha123!"
        );
    }

    public static LoginCommand CreateWithEmail(string email)
    {
        return Create(emailOrUsername: email);
    }

    public static LoginCommand CreateWithUsername(string username)
    {
        return Create(emailOrUsername: username);
    }
}
