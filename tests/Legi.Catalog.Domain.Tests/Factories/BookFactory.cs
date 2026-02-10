using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Domain.Tests.Factories;

public static class BookFactory
{
    private static readonly Guid DefaultCreatedByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Book Create(
        Isbn? isbn = null,
        string? title = null,
        IEnumerable<Author>? authors = null,
        Guid? createdByUserId = null,
        string? synopsis = "A handbook of agile software craftsmanship.",
        int? pageCount = 464,
        string? publisher = "Prentice Hall",
        string? coverUrl = "https://example.com/clean-code.jpg",
        IEnumerable<Tag>? tags = null)
    {
        return Book.Create(
            isbn ?? IsbnFactory.Create(),
            title ?? "Clean Code",
            authors ?? [AuthorFactory.Create()],
            createdByUserId ?? DefaultCreatedByUserId,
            synopsis,
            pageCount,
            publisher,
            coverUrl,
            tags ?? []
        );
    }
}
