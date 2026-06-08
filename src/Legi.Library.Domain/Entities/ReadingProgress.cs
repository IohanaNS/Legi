using Legi.Library.Domain.Events;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class ReadingProgress : BaseAuditableEntity
{
    public const int MaxContentLength = 2000;

    public Guid UserBookId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    public string? Content { get; private set; }
    public bool IsSpoiler { get; private set; }
    public Progress? CurrentProgress { get; private set; }
    public int LikesCount { get; private set; }
    public int CommentsCount { get; private set; }
    public DateOnly ReadingDate { get; private set; }

    public static ReadingProgress Create(
        Guid userBookId,
        Guid userId,
        Guid bookId,
        string? content,
        Progress? progress,
        DateOnly? readingDate = null,
        bool isSpoiler = false)
    {
        if (string.IsNullOrWhiteSpace(content) && progress is null)
            throw new DomainException("Post must have content or progress (or both");

        if (content is not null)
            ValidateContent(content);

        var readingPost = new ReadingProgress
        {
            Id = Guid.NewGuid(),
            UserBookId = userBookId,
            UserId = userId,
            BookId = bookId,
            Content = content,
            IsSpoiler = isSpoiler,
            CurrentProgress = progress,
            ReadingDate = readingDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            LikesCount = 0,
            CommentsCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        readingPost.AddDomainEvent(
            new ReadingProgressCreatedDomainEvent(
                readingPost.Id,
                readingPost.UserBookId,
                readingPost.UserId,
                readingPost.BookId,
                readingPost.Content,
                readingPost.CurrentProgress?.Value,
                readingPost.CurrentProgress?.Type.ToString(),
                readingPost.IsSpoiler));
        return readingPost;
    }

    public void Update(string? content, Progress? progress, bool isSpoiler = false)
    {
        if (string.IsNullOrWhiteSpace(content) && progress is null)
            throw new DomainException("Post must have content or progress (or both)");

        if (content is not null)
            ValidateContent(content);

        Content = content;
        IsSpoiler = isSpoiler;
        CurrentProgress = progress;
        UpdatedAt = DateTime.UtcNow;
    }

    #region Social Counters (updated via integration events from Social)

    public void IncrementLikes() => LikesCount++;

    public void DecrementLikes()
    {
        if (LikesCount > 0) LikesCount--;
    }

    public void IncrementComments() => CommentsCount++;

    public void DecrementComments()
    {
        if (CommentsCount > 0) CommentsCount--;
    }

    #endregion

    public void Delete()
    {
        AddDomainEvent(
            new ReadingPostDeletedDomainEvent(Id, UserId, BookId));
    }

    private static void ValidateContent(string content)
    {
        if (content.Trim().Length > MaxContentLength)
            throw new DomainException($"Post must have at most {MaxContentLength} characters");
    }
}
