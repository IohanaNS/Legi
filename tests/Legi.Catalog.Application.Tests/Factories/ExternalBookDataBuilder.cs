using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Application.Tests.Factories;

public sealed class ExternalBookDataBuilder
{
    private string? _title = "External Clean Code";
    private IReadOnlyList<string>? _authors = ["External Author"];
    private string? _synopsis = "External synopsis.";
    private int? _pageCount = 500;
    private string? _publisher = "External Publisher";
    private string? _coverUrl = "https://example.com/external-cover.jpg";
    private string? _language = "en";
    private string? _providerName = "TestProvider";

    public static ExternalBookDataBuilder Valid() => new();

    public ExternalBookDataBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }

    public ExternalBookDataBuilder WithAuthors(IReadOnlyList<string>? authors)
    {
        _authors = authors;
        return this;
    }

    public ExternalBookDataBuilder WithoutMandatoryData()
    {
        _title = null;
        _authors = null;
        return this;
    }

    public ExternalBookDataBuilder WithSynopsis(string? synopsis)
    {
        _synopsis = synopsis;
        return this;
    }

    public ExternalBookDataBuilder WithPageCount(int? pageCount)
    {
        _pageCount = pageCount;
        return this;
    }

    public ExternalBookDataBuilder WithPublisher(string? publisher)
    {
        _publisher = publisher;
        return this;
    }

    public ExternalBookDataBuilder WithCoverUrl(string? coverUrl)
    {
        _coverUrl = coverUrl;
        return this;
    }

    public ExternalBookData Build()
    {
        return new ExternalBookData
        {
            Title = _title,
            Authors = _authors,
            Synopsis = _synopsis,
            PageCount = _pageCount,
            Publisher = _publisher,
            CoverUrl = _coverUrl,
            Language = _language,
            ProviderName = _providerName
        };
    }
}
