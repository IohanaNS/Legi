namespace Legi.Contracts.Social;

/// <summary>
/// Published when a comment is deleted. Library consumes this to decrement
/// <c>CommentsCount</c> on the target aggregate (Post → ReadingProgress,
/// List → UserList), located via <see cref="TargetType"/> + <see cref="TargetId"/>.
/// <see cref="CommentId"/> / <see cref="UserId"/> are carried for traceability
/// only; Library does not read them.
/// </summary>
public sealed record CommentDeletedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid CommentId,
    Guid UserId
) : IIntegrationEvent;
