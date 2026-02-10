using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Domain.Tests.Factories;

public static class AuthorFactory
{
    public static Author Create(string? name = null)
    {
        return Author.Create(name ?? "Robert C. Martin");
    }

    public static Author CreateAlternative()
    {
        return Author.Create("Martin Fowler");
    }

    public static IReadOnlyList<Author> CreateMany(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => Author.Create($"Author {i}"))
            .ToList();
    }
}
