using Legi.Catalog.Domain.Events;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Entities;

public class Book : BaseAuditableEntity
{
    public const int MaxTags = 30;
    public const int MaxAuthors = 10;

    public Isbn Isbn { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Synopsis { get; private set; }
    public int? PageCount { get; private set; }
    public string? Publisher { get; private set; }
    public string? CoverUrl { get; private set; }
    public decimal AverageRating { get; private set; }
    public int RatingsCount { get; private set; }
    public int ReviewsCount { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<Author> _authors = [];
    public IReadOnlyCollection<Author> Authors => _authors.AsReadOnly();

    private readonly List<Tag> _tags = [];
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Book() { }

    public static Book Create(
        Isbn isbn,
        string title,
        IEnumerable<Author> authors,
        Guid createdByUserId,
        string? synopsis = null,
        int? pageCount = null,
        string? publisher = null,
        string? coverUrl = null,
        IEnumerable<Tag>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required");

        if (title.Length > 500)
               throw new DomainException("Title must be at most 500 characters");

        var authorsList = authors.ToList();
        
        switch (authorsList.Count)
        {
            case 0:
                throw new DomainException("At least one author is required");
            case > MaxAuthors:
                throw new DomainException($"Book cannot have more than {MaxAuthors} authors");
        }

        if (pageCount.HasValue && pageCount.Value <= 0)
            throw new DomainException("Page count must be greater than zero");

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = isbn,
            Title = title.Trim(),
            Synopsis = synopsis?.Trim(),
            PageCount = pageCount,
            Publisher = publisher?.Trim(),
            CoverUrl = coverUrl?.Trim(),
            AverageRating = 0,
            RatingsCount = 0,
            ReviewsCount = 0,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add authors (with duplicate check by slug)
        foreach (var author in authorsList)
        {
            book.AddAuthorInternal(author);
        }

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                book.AddTagInternal(tag);
            }
        }

        book.AddDomainEvent(new BookCreatedDomainEvent(
            book.Id,
            book.Isbn.Value,
            book.Title,
            book._authors.Select(a => a.Name).ToList(),
            book.CreatedByUserId));

        return book;
    }

    #region Author Management

    public void AddAuthor(Author author)
    {
        AddAuthorInternal(author);
        UpdatedAt = DateTime.UtcNow;
    }

    private void AddAuthorInternal(Author author)
    {
        // Check if author already exists (by slug)
        if (_authors.Any(a => a.Slug == author.Slug))
            return; // Silently ignore duplicates

        if (_authors.Count >= MaxAuthors)
            throw new DomainException($"Book cannot have more than {MaxAuthors} authors");

        _authors.Add(author);
    }

    public void RemoveAuthor(Author author)
    {
        if (_authors.Count <= 1)
            throw new DomainException("Book must have at least one author");

        var existingAuthor = _authors.FirstOrDefault(a => a.Slug == author.Slug);
        if (existingAuthor == null) return;
        _authors.Remove(existingAuthor);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAuthors(IEnumerable<Author> authors)
    {
        var authorsList = authors.ToList();

        switch (authorsList.Count)
        {
            case 0:
                throw new DomainException("At least one author is required");
            case > MaxAuthors:
                throw new DomainException($"Book cannot have more than {MaxAuthors} authors");
        }

        _authors.Clear();
        foreach (var author in authorsList)
        {
            AddAuthorInternal(author);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Helper property to get a formatted author string for display.
    /// </summary>
    public string AuthorDisplay => string.Join(", ", _authors.Select(a => a.Name));

    #endregion

    #region Tag Management

    public void AddTag(Tag tag)
    {
        AddTagInternal(tag);
        UpdatedAt = DateTime.UtcNow;
        RaiseTagsUpdatedEvent();
    }

    public void AddTags(IEnumerable<Tag> tags)
    {
        foreach (var tag in tags)
        {
            AddTagInternal(tag);
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseTagsUpdatedEvent();
    }

    private void AddTagInternal(Tag tag)
    {
        // Check if tag already exists (by slug)
        if (_tags.Any(t => t.Slug == tag.Slug))
            return; // Silently ignore duplicates

        if (_tags.Count >= MaxTags)
            throw new DomainException($"Book cannot have more than {MaxTags} tags");

        _tags.Add(tag);
    }

    public void RemoveTag(Tag tag)
    {
        var existingTag = _tags.FirstOrDefault(t => t.Slug == tag.Slug);
        if (existingTag == null) return;
        _tags.Remove(existingTag);
        UpdatedAt = DateTime.UtcNow;
        RaiseTagsUpdatedEvent();
    }

    public void ClearTags()
    {
        _tags.Clear();
        UpdatedAt = DateTime.UtcNow;
        RaiseTagsUpdatedEvent();
    }

    private void RaiseTagsUpdatedEvent()
    {
        AddDomainEvent(new BookTagsUpdatedDomainEvent(
            Id,
            _tags.Select(t => t.Name).ToList()));
    }

    #endregion

    #region Details Update

    public void UpdateDetails(
        string? title = null,
        string? synopsis = null,
        int? pageCount = null,
        string? publisher = null,
        string? coverUrl = null)
    {
        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title cannot be empty");

            if (title.Length > 500)
                throw new DomainException("Title must be at most 500 characters");

            Title = title.Trim();
        }

        if (synopsis != null)
        {
            Synopsis = synopsis.Trim();
        }

        if (pageCount.HasValue)
        {
            if (pageCount.Value <= 0)
                throw new DomainException("Page count must be greater than zero");

            PageCount = pageCount.Value;
        }

        if (publisher != null)
        {
            Publisher = publisher.Trim();
        }

        if (coverUrl != null)
        {
            CoverUrl = coverUrl.Trim();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Rating & Reviews

    /// <summary>
    /// Recalculates the average rating. Called when ratings change in the Library service.
    /// </summary>
    public void RecalculateRating(decimal newAverage, int totalRatings)
    {
        if (newAverage < 0 || newAverage > 5)
            throw new DomainException("Average rating must be between 0 and 5");

        if (totalRatings < 0)
            throw new DomainException("Total ratings cannot be negative");

        AverageRating = Math.Round(newAverage, 2);
        RatingsCount = totalRatings;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookRatingRecalculatedDomainEvent(Id, AverageRating, RatingsCount));
    }

    /// <summary>
    /// Updates review count. Called when reviews are created/deleted in the Library service.
    /// </summary>
    public void UpdateReviewsCount(int count)
    {
        if (count < 0)
            throw new DomainException("Reviews count cannot be negative");

        ReviewsCount = count;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
