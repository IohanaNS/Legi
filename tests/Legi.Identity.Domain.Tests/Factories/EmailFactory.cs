using Legi.Identity.Domain.ValueObjects;

namespace Legi.Identity.Domain.Tests.Factories;

public static class EmailFactory
{
    public static Email Create(string? value = null)
    {
        return Email.Create(value ?? "teste@exemplo.com");
    }

    public static Email CreateRandom()
    {
        var guid = Guid.NewGuid().ToString("N")[..8];
        return Email.Create($"user{guid}@exemplo.com");
    }
}
