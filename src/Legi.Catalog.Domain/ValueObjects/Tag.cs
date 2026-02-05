using System.Text.RegularExpressions;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.ValueObjects;

public sealed class Tag : ValueObject
{
    public string Name { get; }
    public string Slug { get; }

    private Tag(string name, string slug)
    {
        Name = name;
        Slug = slug;
    }

    public static Tag Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tag name is required");

        var trimmedName = name.Trim();

        if (trimmedName.Length < 2)
            throw new DomainException("Tag name must be at least 2 characters");

        if (trimmedName.Length > 50)
            throw new DomainException("Tag name must be at most 50 characters");

        var slug = GenerateSlug(trimmedName);

        return new Tag(trimmedName, slug);
    }

    private static string GenerateSlug(string name)
    {
        // Convert to lowercase
        var slug = name.ToLowerInvariant();

        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");

        // Remove special characters, keep only letters, numbers and hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove multiple consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Slug; // Compare by slug for equality
    }

    public override string ToString() => Name;
}