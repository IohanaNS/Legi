using System.Text.RegularExpressions;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public string Name { get; }
    public string Slug { get; }

    private Author(string name, string slug)
    {
        Name = name;
        Slug = slug;
    }

    public static Author Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Author name is required");

        var trimmedName = name.Trim();

        switch (trimmedName.Length)
        {
            case < 2:
                throw new DomainException("Author name must be at least 2 characters");
            case > 255:
                throw new DomainException("Author name must be at most 255 characters");
            default:
            {
                var slug = GenerateSlug(trimmedName);

                return new Author(trimmedName, slug);
            }
        }
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
        yield return Slug; // Compare by slug for equality (handles "J.K. Rowling" vs "J.K.Rowling")
    }

    public override string ToString() => Name;
}