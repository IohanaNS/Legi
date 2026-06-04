using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.ValueObjects;

namespace Legi.Catalog.Domain.Tests.Factories;

public sealed class BookBuilder
{
    private Isbn _isbn = IsbnFactory.Create();
    private string _title = "Clean Code";
    private IEnumerable<Author> _authors = [AuthorFactory.Create("Robert C. Martin")];
    private Guid _createdByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private string? _synopsis = "A handbook of agile software craftsmanship.";
    private int? _pageCount = 464;
    private string? _publisher = "Prentice Hall";
    private string? _coverUrl = "https://example.com/clean-code.jpg";
    private IEnumerable<Tag>? _tags = [TagFactory.Create("software-engineering")];

    public static BookBuilder Valid() => new();

    public BookBuilder WithIsbn(Isbn isbn)
    {
        _isbn = isbn;
        return this;
    }

    public BookBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public BookBuilder WithAuthors(IEnumerable<Author> authors)
    {
        _authors = authors;
        return this;
    }

    public BookBuilder WithoutAuthors()
    {
        _authors = [];
        return this;
    }

    public BookBuilder WithCreatedByUserId(Guid createdByUserId)
    {
        _createdByUserId = createdByUserId;
        return this;
    }

    public BookBuilder WithSynopsis(string? synopsis)
    {
        _synopsis = synopsis;
        return this;
    }

    public BookBuilder WithPageCount(int? pageCount)
    {
        _pageCount = pageCount;
        return this;
    }

    public BookBuilder WithPublisher(string? publisher)
    {
        _publisher = publisher;
        return this;
    }

    public BookBuilder WithCoverUrl(string? coverUrl)
    {
        _coverUrl = coverUrl;
        return this;
    }

    public BookBuilder WithTags(IEnumerable<Tag>? tags)
    {
        _tags = tags;
        return this;
    }

    public BookBuilder WithoutOptionalMetadata()
    {
        _synopsis = null;
        _pageCount = null;
        _publisher = null;
        _coverUrl = null;
        _tags = null;
        return this;
    }

    public Book Build()
    {
        return Book.Create(
            _isbn,
            _title,
            _authors,
            _createdByUserId,
            _synopsis,
            _pageCount,
            _publisher,
            _coverUrl,
            _tags);
    }
}
