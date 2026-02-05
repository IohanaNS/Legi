using Legi.Catalog.Domain.Events;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Entities;

public class Book : BaseAuditableEntity
{
    public const int MaxTags = 30;

    public Isbn Isbn { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Author { get; private set; } = null!;
    public string? Synopsis { get; private set; }
    public int? PageCount { get; private set; }
    public string? Publisher { get; private set; }
    public string? CoverUrl { get; private set; }
    public decimal AverageRating { get; private set; }
    public int RatingsCount { get; private set; }
    public int ReviewsCount { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private readonly List<Tag> _tags = new();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Book() { }

    public static Book Create(
        Isbn isbn,
        string title,
        string author,
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

        if (string.IsNullOrWhiteSpace(author))
            throw new DomainException("Author is required");

        if (author.Length > 255)
            throw new DomainException("Author must be at most 255 characters");

        if (pageCount.HasValue && pageCount.Value <= 0)
            throw new DomainException("Page count must be greater than zero");

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = isbn,
            Title = title.Trim(),
            Author = author.Trim(),
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
            book.Author,
            book.CreatedByUserId));

        return book;
    }

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
        if (_tags.Count >= MaxTags)
            throw new DomainException($"Book cannot have more than {MaxTags} tags");

        // Check if tag already exists (by slug)
        if (_tags.Any(t => t.Slug == tag.Slug))
            return; // Silently ignore duplicates

        _tags.Add(tag);
    }

    public void RemoveTag(Tag tag)
    {
        var existingTag = _tags.FirstOrDefault(t => t.Slug == tag.Slug);
        if (existingTag != null)
        {
            _tags.Remove(existingTag);
            UpdatedAt = DateTime.UtcNow;
            RaiseTagsUpdatedEvent();
        }
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

    public void UpdateDetails(
        string? title = null,
        string? author = null,
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

        if (author != null)
        {
            if (string.IsNullOrWhiteSpace(author))
                throw new DomainException("Author cannot be empty");

            if (author.Length > 255)
                throw new DomainException("Author must be at most 255 characters");

            Author = author.Trim();
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
}
