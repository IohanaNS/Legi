using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.ValueObjects;

namespace Legi.Identity.Domain.Tests.Factories;

public static class UserFactory
{
    public static User Create(
        Email? email = null,
        Username? username = null,
        string? passwordHash = null,
        string? name = null)
    {
        return User.Create(
            email ?? EmailFactory.Create(),
            username ?? UsernameFactory.Create(),
            passwordHash ?? "hashed_password",
            name ?? "Usuário Teste"
        );
    }

    public static User CreateWithEmail(string email)
    {
        return Create(email: EmailFactory.Create(email));
    }

    public static User CreateWithUsername(string username)
    {
        return Create(username: UsernameFactory.Create(username));
    }

    public static User CreateRandom()
    {
        return Create(
            email: EmailFactory.CreateRandom(),
            username: UsernameFactory.CreateRandom()
        );
    }
}
