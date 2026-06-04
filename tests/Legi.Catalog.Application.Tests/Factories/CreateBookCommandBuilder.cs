using Legi.Catalog.Application.Books.Commands.CreateBook;

namespace Legi.Catalog.Application.Tests.Factories;

public sealed class CreateBookCommandBuilder
{
    private string _isbn = "9780132350884";
    private string _title = "Clean Code";
    private List<string>? _authors = ["Robert C. Martin"];
    private Guid _createdByUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private string? _synopsis = "A handbook of agile software craftsmanship.";
    private int? _pageCount = 464;
    private string? _publisher = "Prentice Hall";
    private string? _coverUrl = "https://example.com/clean-code.jpg";
    private List<string>? _tags = ["software-engineering", "architecture"];

    public static CreateBookCommandBuilder Valid() => new();

    public CreateBookCommandBuilder WithIsbn(string isbn)
    {
        _isbn = isbn;
        return this;
    }

    public CreateBookCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateBookCommandBuilder WithAuthors(List<string>? authors)
    {
        _authors = authors;
        return this;
    }

    public CreateBookCommandBuilder WithoutAuthors()
    {
        _authors = null;
        return this;
    }

    public CreateBookCommandBuilder WithCreatedByUserId(Guid createdByUserId)
    {
        _createdByUserId = createdByUserId;
        return this;
    }

    public CreateBookCommandBuilder WithSynopsis(string? synopsis)
    {
        _synopsis = synopsis;
        return this;
    }

    public CreateBookCommandBuilder WithPageCount(int? pageCount)
    {
        _pageCount = pageCount;
        return this;
    }

    public CreateBookCommandBuilder WithPublisher(string? publisher)
    {
        _publisher = publisher;
        return this;
    }

    public CreateBookCommandBuilder WithCoverUrl(string? coverUrl)
    {
        _coverUrl = coverUrl;
        return this;
    }

    public CreateBookCommandBuilder WithTags(List<string>? tags)
    {
        _tags = tags;
        return this;
    }

    public CreateBookCommandBuilder WithoutOptionalMetadata()
    {
        _synopsis = null;
        _pageCount = null;
        _publisher = null;
        _coverUrl = null;
        _tags = null;
        return this;
    }

    public CreateBookCommand Build()
    {
        return new CreateBookCommand(
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
