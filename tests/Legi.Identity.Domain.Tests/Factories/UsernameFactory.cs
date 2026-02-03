using Legi.Identity.Domain.ValueObjects;

namespace Legi.Identity.Domain.Tests.Factories;

public static class UsernameFactory
{
    public static Username Create(string? value = null)
    {
        return Username.Create(value ?? "testusr");
    }

    public static Username CreateRandom()
    {
        var guid = Guid.NewGuid().ToString("N")[..8];
        return Username.Create($"user{guid}");
    }
}
