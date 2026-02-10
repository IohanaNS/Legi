using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Domain.Tests.Factories;

public static class IsbnFactory
{
    public const string DefaultIsbn13 = "9780132350884";
    public const string DefaultIsbn10 = "0132350882";

    public static Isbn Create(string? value = null)
    {
        return Isbn.Create(value ?? DefaultIsbn13);
    }

    public static Isbn CreateIsbn10()
    {
        return Isbn.Create(DefaultIsbn10);
    }
}
