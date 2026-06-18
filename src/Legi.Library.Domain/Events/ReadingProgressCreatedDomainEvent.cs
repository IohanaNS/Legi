using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

public sealed class ReadingProgressCreatedDomainEvent(
    Guid readingPostId,
    Guid userBookId,
    Guid userId,
    Guid bookId,
    Guid workId,
    string? content,
    int? progressValue,
    string? progressType,
    bool isSpoiler = false)
    : IDomainEvent
{
    public Guid ReadingPostId { get; } = readingPostId;
    public Guid UserBookId { get; } = userBookId;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public Guid WorkId { get; } = workId;
    public string? Content { get; } = content;
    public int? ProgressValue { get; } = progressValue;
    public bool IsSpoiler { get; } = isSpoiler;

    /// <summary>
    /// String form of <see cref="Legi.Library.Domain.Enums.ProgressType"/>
    /// ("Page" or "Percentage"), or null when no progress was supplied. Kept as
    /// string so the cross-context integration contract isn't tied to the enum.
    /// </summary>
    public string? ProgressType { get; } = progressType;

    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
