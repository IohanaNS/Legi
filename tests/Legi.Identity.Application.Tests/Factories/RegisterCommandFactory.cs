using Legi.Identity.Application.Auth.Commands.Register;

namespace Legi.Identity.Application.Tests.Factories;

public static class RegisterCommandFactory
{
    public static RegisterCommand Create(
        string? email = null,
        string? username = null,
        string? password = null)
    {
        return new RegisterCommand(
            email ?? "teste@exemplo.com",
            username ?? "testusr",
            password ?? "Senha123!"
        );
    }

    public static RegisterCommand CreateWithEmail(string email)
    {
        return Create(email: email);
    }

    public static RegisterCommand CreateWithUsername(string username)
    {
        return Create(username: username);
    }

    public static RegisterCommand CreateRandom()
    {
        var guid = Guid.NewGuid().ToString("N")[..8];
        return Create(
            email: $"user{guid}@exemplo.com",
            username: $"user{guid}"
        );
    }
}
