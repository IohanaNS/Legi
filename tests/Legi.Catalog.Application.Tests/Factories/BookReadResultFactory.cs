using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Tests.Factories;

public static class BookReadResultFactory
{
    private static readonly Guid DefaultBookId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DefaultCreatedByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime DefaultCreatedAt = new(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultUpdatedAt = new(2025, 1, 11, 12, 0, 0, DateTimeKind.Utc);

    public static BookDetailsResult CreateDetails(
        Guid? id = null,
        string? isbn = null,
        string? title = null,
        List<(string Name, string Slug)>? authors = null,
        string? synopsis = "A handbook of agile software craftsmanship.",
        int? pageCount = 464,
        string? publisher = "Prentice Hall",
        string? coverUrl = "https://example.com/clean-code.jpg",
        decimal averageRating = 4.5m,
        int ratingsCount = 100,
        int reviewsCount = 20,
        List<(string Name, string Slug)>? tags = null,
        Guid? createdByUserId = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        return new BookDetailsResult(
            id ?? DefaultBookId,
            isbn ?? "9780132350884",
            title ?? "Clean Code",
            authors ?? [("Robert C. Martin", "robert-c-martin")],
            synopsis,
            pageCount,
            publisher,
            coverUrl,
            averageRating,
            ratingsCount,
            reviewsCount,
            tags ?? [("software-engineering", "software-engineering")],
            createdByUserId ?? DefaultCreatedByUserId,
            createdAt ?? DefaultCreatedAt,
            updatedAt ?? DefaultUpdatedAt
        );
    }

    public static BookSearchResult CreateSearchResult(
        Guid? id = null,
        string? isbn = null,
        string? title = null,
        List<(string Name, string Slug)>? authors = null,
        string? coverUrl = "https://example.com/clean-code.jpg",
        decimal averageRating = 4.5m,
        int ratingsCount = 100,
        List<(string Name, string Slug)>? tags = null)
    {
        return new BookSearchResult(
            id ?? DefaultBookId,
            isbn ?? "9780132350884",
            title ?? "Clean Code",
            authors ?? [("Robert C. Martin", "robert-c-martin")],
            coverUrl,
            averageRating,
            ratingsCount,
            tags ?? [("software-engineering", "software-engineering")]
        );
    }
}
