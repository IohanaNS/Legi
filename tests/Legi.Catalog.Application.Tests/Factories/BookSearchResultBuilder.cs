using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Tests.Factories;

public sealed class BookSearchResultBuilder
{
    private Guid _id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private string _isbn = "9780132350884";
    private string _title = "Clean Code";
    private List<(string Name, string Slug)> _authors = [("Robert C. Martin", "robert-c-martin")];
    private string? _coverUrl = "https://example.com/clean-code.jpg";
    private decimal _averageRating = 4.5m;
    private int _ratingsCount = 120;
    private List<(string Name, string Slug)> _tags = [("software-engineering", "software-engineering")];

    public static BookSearchResultBuilder Valid() => new();

    public BookSearchResultBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public BookSearchResultBuilder WithIsbn(string isbn)
    {
        _isbn = isbn;
        return this;
    }

    public BookSearchResultBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public BookSearchResultBuilder WithAuthors(List<(string Name, string Slug)> authors)
    {
        _authors = authors;
        return this;
    }

    public BookSearchResultBuilder WithTags(List<(string Name, string Slug)> tags)
    {
        _tags = tags;
        return this;
    }

    public BookSearchResultBuilder WithCoverUrl(string? coverUrl)
    {
        _coverUrl = coverUrl;
        return this;
    }

    public BookSearchResult Build()
    {
        return new BookSearchResult(
            _id,
            _isbn,
            _title,
            _authors,
            _coverUrl,
            _averageRating,
            _ratingsCount,
            _tags);
    }
}
