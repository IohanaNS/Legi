using Legi.SharedKernel;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;

namespace Legi.Social.Domain.Entities;

public class Comment : BaseEntity
{
    public const int MinContentLength = 1;
    public const int MaxContentLength = 500;

    public Guid UserId { get; private set; }
    public InteractableType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public static Comment Create(
        Guid userId,
        InteractableType targetType,
        Guid targetId,
        string content)
    {
        ValidateContent(content);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        comment.AddDomainEvent(
            new CommentCreatedDomainEvent(comment.Id, userId, targetType, targetId));
        return comment;
    }

    /// <summary>
    /// Marks this comment for deletion, raising the domain event
    /// so the Library can decrement CommentsCount on the target content.
    /// Authorization (author or content owner) is validated in the handler.
    /// </summary>
    public void MarkForDeletion()
    {
        AddDomainEvent(
            new CommentDeletedDomainEvent(Id, UserId, TargetType, TargetId));
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment content cannot be empty");

        if (content.Length > MaxContentLength)
            throw new DomainException(
                $"Comment content cannot exceed {MaxContentLength} characters");
    }
}