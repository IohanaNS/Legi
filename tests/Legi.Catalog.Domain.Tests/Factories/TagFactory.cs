using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Domain.Tests.Factories;

public static class TagFactory
{
    public static Tag Create(string? name = null)
    {
        return Tag.Create(name ?? "software-engineering");
    }

    public static Tag CreateAlternative()
    {
        return Tag.Create("architecture");
    }

    public static IReadOnlyList<Tag> CreateMany(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => Tag.Create($"tag-{i}"))
            .ToList();
    }
}
