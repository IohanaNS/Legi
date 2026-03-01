using Legi.Library.Domain.Events;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class ReadingPost : BaseAuditableEntity
{
    public Guid UserBookId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    public string? Content { get; private set; }
    public Progress? CurrentProgress { get; private set; }
    public int LikesCount { get; private set; }
    public int CommentsCount { get; private set; }
    public DateOnly ReadingDate { get; private set; }

    public static ReadingPost Create(
        Guid userId,
        Guid bookId,
        string? content,
        Progress? progress,
        DateOnly? readingDate = null)
    {
        if (string.IsNullOrWhiteSpace(content) && progress is null)
            throw new DomainException("Post must have content or progress (or both");

        if (content is not null)
            ValidateContent(content);

        var readingPost = new ReadingPost
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BookId = bookId,
            Content = content,
            CurrentProgress = progress,
            ReadingDate = readingDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            LikesCount = 0,
            CommentsCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        readingPost.AddDomainEvent(
            new ReadingPostCreatedDomainEvent(readingPost.Id, readingPost.UserBookId, readingPost.UserId,
                readingPost.BookId));
        return readingPost;
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
        const int maxContentLength = 2000;
        if (content.Trim().Length > maxContentLength)
            throw new DomainException($"Post must have at most {maxContentLength} characters");
    }
}