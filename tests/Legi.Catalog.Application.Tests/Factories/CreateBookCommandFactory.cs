using Legi.Catalog.Application.Books.Commands.CreateBook;

namespace Legi.Catalog.Application.Tests.Factories;

public static class CreateBookCommandFactory
{
    private static readonly Guid DefaultCreatedByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static CreateBookCommand Create(
        string? isbn = null,
        string? title = null,
        List<string>? authors = null,
        Guid? createdByUserId = null,
        string? synopsis = "A handbook of agile software craftsmanship.",
        int? pageCount = 464,
        string? publisher = "Prentice Hall",
        string? coverUrl = "https://example.com/clean-code.jpg",
        List<string>? tags = null)
    {
        return new CreateBookCommand(
            isbn ?? "9780132350884",
            title ?? "Clean Code",
            authors ?? ["Robert C. Martin"],
            createdByUserId ?? DefaultCreatedByUserId,
            synopsis,
            pageCount,
            publisher,
            coverUrl,
            tags ?? ["software-engineering", "architecture"]
        );
    }
}
