using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Comment comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages deletion of every Comment on (targetType, targetId) in the change
    /// tracker without saving. Used when content is deleted upstream and its
    /// comments must be purged. Loads then removes via the tracker — never
    /// ExecuteDelete — so it commits atomically with the inbox row
    /// (decision 8.1.3). Does NOT raise CommentDeleted domain events: this is a
    /// cascade from content deletion, not a user action (decision 8.1.2).
    /// Idempotent: no rows is a no-op.
    /// </summary>
    Task StageDeleteByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
}