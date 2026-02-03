using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.ValueObjects;

namespace Legi.Identity.Application.Tests.Factories;

public static class UserFactory
{
    public static User Create(
        Email? email = null,
        Username? username = null,
        string? passwordHash = null,
        string? name = null)
    {
        return User.Create(
            email ?? Email.Create("teste@exemplo.com"),
            username ?? Username.Create("testusr"),
            passwordHash ?? "hashed_password",
            name ?? "Usuário Teste"
        );
    }

    public static User CreateWithEmail(string email)
    {
        return Create(email: Email.Create(email));
    }

    public static User CreateWithUsername(string username)
    {
        return Create(username: Username.Create(username));
    }

    public static User CreateRandom()
    {
        var guid = Guid.NewGuid().ToString("N")[..8];
        return Create(
            email: Email.Create($"user{guid}@exemplo.com"),
            username: Username.Create($"user{guid}")
        );
    }
}
